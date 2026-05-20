using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Locking;

/// <summary>
///     redis分布式锁，支持自动续期。
/// </summary>
internal sealed class RedisLock : ILocalReentryAwareLockHandler
{
    private int MaxConsecutiveFailureCount = 10; // 续期连续失败上限，超过后放弃锁

    // Lua 脚本：只有当 key 的值等于 ARGV[1] 时，才设置过期（毫秒）
    private const string RenewScript = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('pexpire', KEYS[1], ARGV[2])
else
    return 0
end";

    // Lua 脚本：只有当 key 的值等于 ARGV[1] 时，才删除 key
    private const string ReleaseScript = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
else
    return 0
end";

    private readonly IDatabase _db;
    private readonly bool _enableAutoRenew;
    private readonly TimeSpan _expiry;
    private readonly TimeSpan _interval;
    private readonly string _lockKey;
    private readonly string _lockValue = Guid.NewGuid().ToString().Replace("-", string.Empty);
    private readonly ILogger _logger;
    private readonly int? _maxRenewalCount;

    // 通过 int + Interlocked 保证原子性与可见性（0 = false, 1 = true）
    private int _acquired; // 0/1
    private int _consecutiveFailureCount; // 续期连续失败次数，成功后归零
    private CancellationTokenSource _cts;
    private long _lastSuccessfulRenewalMs; // 最后一次成功续期的 TickCount64 毫秒值，0 表示从未续期
    private int _renewedCount;

    /// <summary>
    ///     构造 RedisLock 实例。
    ///     expiry 表示锁在 Redis 中的过期时间（例如 3s）。不再强制为 3 的倍数。
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="db">Redis 数据库实例。</param>
    /// <param name="lockKey">锁的键名。</param>
    /// <param name="expiry">锁的过期时间。</param>
    /// <param name="enableAutoRenew">是否自动续期。</param>
    /// <param name="maxRenewalCount">最大续期次数。null 表示无限续期。</param>
    public RedisLock(ILogger logger, IDatabase db, string lockKey, TimeSpan expiry, bool enableAutoRenew,
        int? maxRenewalCount)
    {
        _logger = logger;
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        _enableAutoRenew = enableAutoRenew;
        _expiry = expiry;
        _maxRenewalCount = maxRenewalCount;

        if (_expiry <= TimeSpan.Zero)
        {
            throw new ArgumentException("必须设置有效的过期时间", nameof(expiry));
        }

        if (_maxRenewalCount.HasValue && _maxRenewalCount.Value <= 0)
        {
            throw new ArgumentException("最大续期次数必须大于0", nameof(maxRenewalCount));
        }

        // 以 ticks 精确计算三等分，避免浮点误差；确保最小间隔（比如 50ms）
        long ticks = _expiry.Ticks / 3;
        if (ticks <= 0)
        {
            ticks = 1;
        }

        long minInterval = TimeSpan.FromMilliseconds(50).Ticks;
        if (ticks < minInterval)
        {
            ticks = minInterval;
        }

        _interval = TimeSpan.FromTicks(ticks);

        _acquired = 0;

        _logger.LogDebug(
            "Redis锁handler初始化 | LockKey={LockKey} | LockToken={LockToken} | Expiry={Expiry} | AutoRenew={AutoRenew} | MaxRenewalCount={MaxRenewalCount}",
            _lockKey, GetLockTokenForLog(), _expiry, _enableAutoRenew, _maxRenewalCount);
    }

    /// <summary>
    ///     当前锁是否已获取。
    /// </summary>
    public bool Acquired => Volatile.Read(ref _acquired) == 1;

    public ValueTask<bool> TryReenterAsync(CancellationToken cancellationToken = default)
    {
        if (!Acquired)
        {
            return ValueTask.FromResult(false);
        }

        if (_enableAutoRenew)
        {
            // 后台续期循环持续刷新时间戳，若在 _expiry 窗口内则锁一定还属于本实例
            long lastMs = Volatile.Read(ref _lastSuccessfulRenewalMs);
            if (lastMs > 0 && Environment.TickCount64 - lastMs < _expiry.TotalMilliseconds)
            {
                return ValueTask.FromResult(true);
            }
        }

        // 无续期循环（FixedExpiry）或时间戳过期（续期异常/配额耗尽），回退到 Redis 验证并延长 TTL
        return TryRenewAsync(cancellationToken);
    }

    /// <summary>
    ///     异步释放锁（原子 compare-and-del）。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 仅第一个释放调用会继续执行删除逻辑
        if (Interlocked.Exchange(ref _acquired, 0) == 1)
        {
            // 取走并取消 cts（线程安全）
            CancellationTokenSource cts = Interlocked.Exchange(ref _cts, null);
            try
            {
                cts?.Cancel();
            }
            catch
            {
            }

            cts?.Dispose();

            try
            {
                _logger.LogDebug("释放Redis锁 | LockKey={LockKey} | LockToken={LockToken} | State=Releasing", _lockKey, GetLockTokenForLog());
                bool released = await ReleaseAsync().ConfigureAwait(false);
                if (released)
                {
                    _logger.LogDebug("Redis锁释放完成 | LockKey={LockKey}", _lockKey);
                }
                else
                {
                    _logger.LogWarning("Redis锁释放时未删除键，锁可能已失去所有权 | LockKey={LockKey}", _lockKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Redis锁释放失败 | LockKey={LockKey} | LockToken={LockToken} | ErrorMessage={ErrorMessage}",
                    _lockKey, GetLockTokenForLog(), ex.Message);
            }
        }
        else
        {
            // 即使未持有，也要确保可能残留的 cts 被释放
            CancellationTokenSource leftover = Interlocked.Exchange(ref _cts, null);
            leftover?.Dispose();
        }
    }

    /// <summary>
    ///     异步尝试获取锁，并自动续期。
    /// </summary>
    /// <returns>返回自身实例。</returns>
    public async Task<RedisLock> LockAsync(int attempt = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("尝试获取Redis锁 | LockKey={LockKey} | Attempt={Attempt} | Expiry={Expiry} | AutoRenew={AutoRenew} | MaxRenewalCount={MaxRenewalCount}",
            _lockKey, attempt, _expiry, _enableAutoRenew, _maxRenewalCount);
        bool got = await _db.StringSetAsync(_lockKey, _lockValue, _expiry, When.NotExists).ConfigureAwait(false);
        _logger.LogDebug("Redis锁获取结果 | LockKey={LockKey} | Attempt={Attempt} | Got={Got}", _lockKey, attempt,
            got);
        if (got)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("获取Redis锁后发现请求已取消，立即回滚锁 | LockKey={LockKey} | Attempt={Attempt}", _lockKey,
                    attempt);
                await ReleaseAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // 原子标记已获取
            Interlocked.Exchange(ref _acquired, 1);
            Volatile.Write(ref _lastSuccessfulRenewalMs, Environment.TickCount64);

            if (_enableAutoRenew)
            {
                // 创建并发布续期任务（不等待）
                CancellationTokenSource cts = new();
                // 以原子方式设置 _cts，防止竞态释放时访问到旧实例
                CancellationTokenSource old = Interlocked.Exchange(ref _cts, cts);
                old?.Dispose();

                _ = StartRenewalAsync(cts.Token);
            }

            _logger.LogDebug("Redis锁获取成功 | LockKey={LockKey} | Attempt={Attempt} | AutoRenew={AutoRenew} | MaxRenewalCount={MaxRenewalCount}",
                _lockKey, attempt, _enableAutoRenew, _maxRenewalCount);
        }

        return this;
    }

    /// <summary>
    ///     后台自动续期任务（安全：仅当值匹配时才续期）。
    /// </summary>
    /// <param name="token">取消令牌。</param>
    private async Task StartRenewalAsync(CancellationToken token)
    {
        _logger.LogDebug("Redis锁续期任务启动 | LockKey={LockKey} | Interval={Interval} | Expiry={Expiry} | MaxRenewalCount={MaxRenewalCount}",
            _lockKey, _interval, _expiry, _maxRenewalCount);

        while (Volatile.Read(ref _acquired) == 1 && !token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token).ConfigureAwait(false);

                if (!await TryRenewAsync(token).ConfigureAwait(false))
                {
                    break;
                }

                _consecutiveFailureCount = 0;
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                int count = Interlocked.Increment(ref _consecutiveFailureCount);
                if (count >= MaxConsecutiveFailureCount)
                {
                    _logger.LogWarning(ex, "Redis锁续期连续失败已达上限({MaxFailureCount})，放弃锁 | LockKey={LockKey}",
                        MaxConsecutiveFailureCount, _lockKey);
                    MarkLostOwnership();
                    break;
                }

                // 指数退避：100ms × 2^n，上限为续期间隔，避免无退避重试导致日志洪水
                double backoffMs = Math.Min(_interval.TotalMilliseconds, 100d * Math.Pow(2, Math.Min(count, 5)));
                if (backoffMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(backoffMs), token).ConfigureAwait(false);
                }

                _logger.LogError(ex,
                    "Redis锁续期异常 | LockKey={LockKey} | LockToken={LockToken} | Error={Error} | FailureCount={FailureCount} | RenewedCount={RenewedCount}",
                    _lockKey, GetLockTokenForLog(), ex.Message, count, Volatile.Read(ref _renewedCount));
            }
        }

        _logger.LogDebug(
            "Redis锁续期任务停止 | LockKey={LockKey} | Acquired={Acquired} | CancellationRequested={CancellationRequested} | RenewedCount={RenewedCount}",
            _lockKey,
            Acquired,
            token.IsCancellationRequested,
            Volatile.Read(ref _renewedCount));
    }

    private async Task<bool> ReleaseAsync()
    {
        // 原子删除：只有当 value 匹配时才删除
        long result = (long)await _db.ScriptEvaluateAsync(
            ReleaseScript,
            new RedisKey[] { _lockKey },
            new RedisValue[] { _lockValue }).ConfigureAwait(false);
        return result > 0;
    }

    private async ValueTask<bool> TryRenewAsync(CancellationToken cancellationToken)
    {
        // 若 CTS 已取消，立即抛出，避免在锁已释放后继续发起 Redis 调用
        cancellationToken.ThrowIfCancellationRequested();

        if (!Acquired)
        {
            return false;
        }

        // 调 TryReserveRenewalQuota 而非 _maxRenewalCount.HasValue，因为配额准入逻辑可能未来扩展
        if (!TryReserveRenewalQuota())
        {
            _logger.LogDebug("Redis锁续期次数已达上限 | 键名={LockKey} | 已续期次数={RenewedCount}", _lockKey,
                Volatile.Read(ref _renewedCount));
            return false;
        }

        // 只有有限配额模式下才真正预留了配额，无限模式/null 时 TryReserveRenewalQuota 直接返回 true 未预留
        bool didReserve = _maxRenewalCount.HasValue;
        try
        {
            // 原子续期：仅当 Redis 中锁值仍匹配时才 PEXPIRE
            long result = (long)await _db.ScriptEvaluateAsync(
                RenewScript,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue, (long)_expiry.TotalMilliseconds }).ConfigureAwait(false);

            _logger.LogDebug("Redis锁续期结果 | 键名={LockKey} | 结果={Result}", _lockKey, result);
            if (result == 0)
            {
                // 锁值不匹配：锁已被抢占，回滚预留的配额
                if (didReserve)
                {
                    Interlocked.Decrement(ref _renewedCount);
                }

                _logger.LogDebug(
                    "Redis锁续期失败 | LockKey={LockKey} | Reason=LostOwnership | QuotaReserved={QuotaReserved} | RenewedCount={RenewedCount}",
                    _lockKey, didReserve, Volatile.Read(ref _renewedCount));
                MarkLostOwnership();
                return false;
            }

            if (didReserve)
            {
                _logger.LogDebug("Redis锁续期成功并消耗次数 | 键名={LockKey} | 已续期次数={RenewedCount}", _lockKey,
                    Volatile.Read(ref _renewedCount));
            }

            // 记录成功时间戳，供 TryReenterAsync 做快速所有权判断
            Volatile.Write(ref _lastSuccessfulRenewalMs, Environment.TickCount64);
            return true;
        }
        catch
        {
            // Redis 调用异常时回滚预留的配额
            if (didReserve)
            {
                Interlocked.Decrement(ref _renewedCount);
            }

            throw;
        }
    }

    private bool TryReserveRenewalQuota()
    {
        if (!_maxRenewalCount.HasValue)
        {
            return true;
        }

        while (true)
        {
            int current = Volatile.Read(ref _renewedCount);
            if (current >= _maxRenewalCount.Value)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _renewedCount, current + 1, current) == current)
            {
                return true;
            }
        }
    }

    private void MarkLostOwnership()
    {
        if (Interlocked.Exchange(ref _acquired, 0) != 1)
        {
            return;
        }

        Volatile.Write(ref _lastSuccessfulRenewalMs, 0);
        _logger.LogWarning("Redis锁已失去所有权 | LockKey={LockKey} | LockToken={LockToken}", _lockKey, GetLockTokenForLog());

        CancellationTokenSource cts = Interlocked.Exchange(ref _cts, null);
        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        cts?.Dispose();
    }

    private string GetLockTokenForLog()
    {
        return _lockValue.Length <= 8 ? _lockValue : _lockValue[..8];
    }
}

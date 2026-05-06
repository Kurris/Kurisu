using System;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Locking;

/// <summary>
/// redis分布式锁，支持自动续期。
/// </summary>
internal sealed class RedisLock : ILocalReentryAwareLockHandler
{
    private readonly ILogger _logger;
    private readonly IDatabase _db;
    private readonly string _lockKey;
    private readonly string _lockValue = Guid.NewGuid().ToString().Replace("-", string.Empty);
    private readonly TimeSpan _expiry;
    private readonly TimeSpan _interval;
    private readonly bool _enableAutoRenew;
    private readonly int? _maxRenewalCount;

    // 通过 int + Interlocked 保证原子性与可见性（0 = false, 1 = true）
    private int _acquired; // 0/1
    private int _renewedCount;
    private CancellationTokenSource _cts;

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

    /// <summary>
    /// 构造 RedisLock 实例。
    /// expiry 表示锁在 Redis 中的过期时间（例如 3s）。不再强制为 3 的倍数。
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="db">Redis 数据库实例。</param>
    /// <param name="lockKey">锁的键名。</param>
    /// <param name="expiry">锁的过期时间。</param>
    /// <param name="enableAutoRenew">是否自动续期。</param>
    /// <param name="maxRenewalCount">最大续期次数。null 表示无限续期。</param>
    public RedisLock(ILogger logger, IDatabase db, string lockKey, TimeSpan expiry, bool enableAutoRenew, int? maxRenewalCount)
    {
        _logger = logger;
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        _enableAutoRenew = enableAutoRenew;
        _expiry = expiry;
        _maxRenewalCount = maxRenewalCount;

        if (_expiry <= TimeSpan.Zero)
            throw new ArgumentException("必须设置有效的过期时间", nameof(expiry));

        if (_maxRenewalCount.HasValue && _maxRenewalCount.Value <= 0)
            throw new ArgumentException("最大续期次数必须大于0", nameof(maxRenewalCount));

        // 以 ticks 精确计算三等分，避免浮点误差；确保最小间隔（比如 50ms）
        var ticks = _expiry.Ticks / 3;
        if (ticks <= 0) ticks = 1;
        var minInterval = TimeSpan.FromMilliseconds(50).Ticks;
        if (ticks < minInterval) ticks = minInterval;
        _interval = TimeSpan.FromTicks(ticks);

        _acquired = 0;

        _logger.LogDebug("Redis锁handler初始化 | 键名={LockKey} | 锁值={LockValue} | 过期时间={Expiry} | 自动续期={AutoRenew} | 最大续期次数={MaxRenewalCount}", _lockKey, _lockValue, _expiry, _enableAutoRenew, _maxRenewalCount);
    }

    /// <summary>
    /// 当前锁是否已获取。
    /// </summary>
    public bool Acquired => Volatile.Read(ref _acquired) == 1;

    /// <summary>
    /// 异步尝试获取锁，并自动续期。
    /// </summary>
    /// <returns>返回自身实例。</returns>
    public async Task<RedisLock> LockAsync(int attempt = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("尝试获取Redis锁 | 键名={LockKey} | 尝试次数={Attempt} | 状态: 尝试中", _lockKey, attempt);
        var got = await _db.StringSetAsync(_lockKey, _lockValue, _expiry, when: When.NotExists).ConfigureAwait(false);
        _logger.LogDebug("Redis锁获取结果 | 键名={LockKey} | 尝试次数={Attempt} | 是否成功={Got} | 请检查: 锁是否被抢占", _lockKey, attempt, got);
        if (got)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("获取Redis锁后发现请求已取消，立即回滚锁 | LockKey={LockKey} | Attempt={Attempt}", _lockKey, attempt);
                await ReleaseAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // 原子标记已获取
            Interlocked.Exchange(ref _acquired, 1);

            if (_enableAutoRenew)
            {
                // 创建并发布续期任务（不等待）
                var cts = new CancellationTokenSource();
                // 以原子方式设置 _cts，防止竞态释放时访问到旧实例
                var old = Interlocked.Exchange(ref _cts, cts);
                old?.Dispose();

                _ = StartRenewalAsync(cts.Token);
            }

            _logger.LogInformation("Redis锁获取成功 | LockKey={LockKey} | Attempt={Attempt} | AutoRenew={AutoRenew}", _lockKey, attempt, _enableAutoRenew);
        }

        return this;
    }

    public ValueTask<bool> TryReenterAsync(CancellationToken cancellationToken = default)
    {
        if (!Acquired)
        {
            return ValueTask.FromResult(false);
        }

        if (_enableAutoRenew && !_maxRenewalCount.HasValue)
        {
            return ValueTask.FromResult(true);
        }

        return TryRenewAsync(consumeQuota: _maxRenewalCount.HasValue, cancellationToken);
    }

    /// <summary>
    /// 后台自动续期任务（安全：仅当值匹配时才续期）。
    /// </summary>
    /// <param name="token">取消令牌。</param>
    private async Task StartRenewalAsync(CancellationToken token)
    {
        _logger.LogDebug("Redis锁续期任务启动 | 键名={LockKey} | 续期间隔={Interval} | 安全提示: 避免锁过期", _lockKey, _interval);

        var expiryMillis = (long)_expiry.TotalMilliseconds;
        while (Volatile.Read(ref _acquired) == 1 && !token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token).ConfigureAwait(false);

                if (!await TryRenewAsync(consumeQuota: _maxRenewalCount.HasValue, token).ConfigureAwait(false))
                {
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis锁续期异常 | 键名={LockKey} | 锁值={LockValue} | 原因:{error}", _lockKey, _lockValue, ex.Message);
            }
        }

        _logger.LogInformation(
            "Redis锁续期任务停止 | LockKey={LockKey} | Acquired={Acquired} | CancellationRequested={CancellationRequested} | RenewedCount={RenewedCount}",
            _lockKey,
            Acquired,
            token.IsCancellationRequested,
            Volatile.Read(ref _renewedCount));
    }

    /// <summary>
    /// 异步释放锁（原子 compare-and-del）。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 仅第一个释放调用会继续执行删除逻辑
        if (Interlocked.Exchange(ref _acquired, 0) == 1)
        {
            // 取走并取消 cts（线程安全）
            var cts = Interlocked.Exchange(ref _cts, null);
            try { cts?.Cancel(); } catch { }
            cts?.Dispose();

            try
            {
                _logger.LogDebug("释放Redis锁 | 键名={LockKey} | 锁值={LockValue} | 状态: 正在释放", _lockKey, _lockValue);
                var released = await ReleaseAsync().ConfigureAwait(false);
                if (released)
                {
                    _logger.LogInformation("Redis锁释放完成 | LockKey={LockKey}", _lockKey);
                }
                else
                {
                    _logger.LogWarning("Redis锁释放时未删除键，锁可能已失去所有权 | LockKey={LockKey}", _lockKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis锁释放失败 | 键名={LockKey} | 锁值={LockValue} | 错误: {ErrorMessage} | 修复建议: 检查Redis连接或锁值一致性", _lockKey, _lockValue, ex.Message);
            }
        }
        else
        {
            // 即使未持有，也要确保可能残留的 cts 被释放
            var leftover = Interlocked.Exchange(ref _cts, null);
            leftover?.Dispose();
        }
    }

    private async Task<bool> ReleaseAsync()
    {
        // 原子删除：只有当 value 匹配时才删除
        var result = (long)await _db.ScriptEvaluateAsync(
            ReleaseScript,
            new RedisKey[] { _lockKey },
            new RedisValue[] { _lockValue }).ConfigureAwait(false);
        return result > 0;
    }

    private async ValueTask<bool> TryRenewAsync(bool consumeQuota, CancellationToken cancellationToken)
    {
        if (!Acquired)
        {
            return false;
        }

        var quotaReserved = false;
        try
        {
            if (consumeQuota && !TryReserveRenewalQuota())
            {
                _logger.LogDebug("Redis锁续期次数已达上限 | 键名={LockKey} | 已续期次数={RenewedCount}", _lockKey, Volatile.Read(ref _renewedCount));
                return false;
            }

            quotaReserved = consumeQuota;

            var result = (long)await _db.ScriptEvaluateAsync(
                RenewScript,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue, (long)_expiry.TotalMilliseconds }).ConfigureAwait(false);

            _logger.LogDebug("Redis锁续期结果 | 键名={LockKey} | 结果={Result}", _lockKey, result);
            if (result == 0)
            {
                if (quotaReserved)
                {
                    Interlocked.Decrement(ref _renewedCount);
                    quotaReserved = false;
                }

                _logger.LogDebug("Redis锁续期失败 | 键名={LockKey} | 当前Redis锁值={RedisValue} | 本机锁值={LockValue} | 原因: 被其他实例抢占 | 请检查: 锁值一致性", _lockKey, await _db.StringGetAsync(_lockKey), _lockValue);
                MarkLostOwnership();
                return false;
            }

            if (quotaReserved && _maxRenewalCount.HasValue)
            {
                _logger.LogDebug("Redis锁续期成功并消耗次数 | 键名={LockKey} | 已续期次数={RenewedCount}", _lockKey, Volatile.Read(ref _renewedCount));
            }

            return true;
        }
        catch
        {
            if (quotaReserved)
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
            var current = Volatile.Read(ref _renewedCount);
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

        _logger.LogWarning("Redis锁已失去所有权 | LockKey={LockKey} | LockValue={LockValue}", _lockKey, _lockValue);

        var cts = Interlocked.Exchange(ref _cts, null);
        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        cts?.Dispose();
    }
}

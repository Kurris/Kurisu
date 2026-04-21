using System;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Implements;

/// <summary>
/// redis分布式锁，支持自动续期。
/// </summary>
internal sealed class RedisLock : ILockHandler
{
    private readonly ILogger _logger;
    private readonly IDatabase _db;
    private readonly string _lockKey;
    private readonly string _lockValue = Guid.NewGuid().ToString().Replace("-", string.Empty);
    private readonly TimeSpan _expiry;
    private readonly TimeSpan _interval;
    private readonly bool _enableAutoRenew;

    // 通过 int + Interlocked 保证原子性与可见性（0 = false, 1 = true）
    private int _acquired; // 0/1
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
    /// <param name="expiry">锁的过期时间，默认 6 秒。</param>
    public RedisLock(ILogger logger, IDatabase db, string lockKey, TimeSpan? expiry = null)
    {
        _logger = logger;
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        _enableAutoRenew = !expiry.HasValue;
        _expiry = expiry ?? TimeSpan.FromSeconds(6);

        if (_expiry <= TimeSpan.Zero)
            throw new ArgumentException("必须设置有效的过期时间", nameof(expiry));

        // 以 ticks 精确计算三等分，避免浮点误差；确保最小间隔（比如 50ms）
        var ticks = _expiry.Ticks / 3;
        if (ticks <= 0) ticks = 1;
        var minInterval = TimeSpan.FromMilliseconds(50).Ticks;
        if (ticks < minInterval) ticks = minInterval;
        _interval = TimeSpan.FromTicks(ticks);

        _acquired = 0;

        _logger.LogDebug("Redis锁handler初始化 | 键名={LockKey} | 锁值={LockValue} | 过期时间={Expiry} | 自动续期={AutoRenew}", _lockKey, _lockValue, _expiry, _enableAutoRenew);
    }

    /// <summary>
    /// 当前锁是否已获取。
    /// </summary>
    public bool Acquired => Volatile.Read(ref _acquired) == 1;

    /// <summary>
    /// 异步尝试获取锁，并自动续期。
    /// </summary>
    /// <returns>返回自身实例。</returns>
    public async Task<RedisLock> LockAsync()
    {
        _logger.LogDebug("尝试获取Redis锁 | 键名={LockKey} | 状态: 尝试中", _lockKey);
        var got = await _db.StringSetAsync(_lockKey, _lockValue, _expiry, when: When.NotExists).ConfigureAwait(false);
        _logger.LogDebug("Redis锁获取结果 | 键名={LockKey} | 是否成功={Got} | 请检查: 锁是否被抢占", _lockKey, got);
        if (got)
        {
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
        }

        return this;
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
            await Task.Delay(_interval, token).ConfigureAwait(false);

            try
            {
                var result = (long)await _db.ScriptEvaluateAsync(
                    RenewScript,
                    new RedisKey[] { _lockKey },
                    new RedisValue[] { _lockValue, expiryMillis }).ConfigureAwait(false);

                _logger.LogDebug("Redis锁续期结果 | 键名={LockKey} | 结果={Result}", _lockKey, result);
                if (result == 0)
                {
                    _logger.LogDebug("Redis锁续期失败 | 键名={LockKey} | 当前Redis锁值={RedisValue} | 本机锁值={LockValue} | 原因: 被其他实例抢占 | 请检查: 锁值一致性", _lockKey, await _db.StringGetAsync(_lockKey), _lockValue);
                    // 无法续期：可能 key 不存在或值已被替换
                    // 原子清理 acquired 标志并取消/释放 token source（一次性）
                    if (Interlocked.Exchange(ref _acquired, 0) == 1)
                    {
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
                // 原子删除：只有当 value 匹配时才删除
                _ = (long)await _db.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { _lockKey },
                    new RedisValue[] { _lockValue }).ConfigureAwait(false);
                _logger.LogDebug("RedisLock 成功释放: {LockKey}", _lockKey);
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
}
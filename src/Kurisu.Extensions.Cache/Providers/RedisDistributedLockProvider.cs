using System.Diagnostics;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache.Locking;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

/// <summary>
/// Redis 分布式锁提供者。
/// </summary>
public class RedisDistributedLockProvider : ILockable
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisDistributedLockProvider> _logger;
    private readonly RedisReentrantLockContext _reentrantLockContext = new();

    public RedisDistributedLockProvider(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisDistributedLockProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        _logger = logger;
        _db = connectionMultiplexer.GetDatabase();
    }

    public Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TimeModeHandler);
        ArgumentNullException.ThrowIfNull(options.RetryStrategy);

        var scopes = _reentrantLockContext.EnsureScopes();
        var reentrantAttempt = _reentrantLockContext.TryEnterAsync(lockKey, cancellationToken);
        if (reentrantAttempt.IsCompletedSuccessfully)
        {
            var reentrantHandler = reentrantAttempt.Result;
            if (reentrantHandler != null)
            {
                _logger.LogDebug("同一请求内复用锁 | LockKey={LockKey} | ReentryMode=ReuseFirstLock | SkipRedisAcquire=true", lockKey);
                return Task.FromResult(reentrantHandler);
            }

            return LockCoreAsync(lockKey, options, scopes, cancellationToken);
        }

        return LockWithReentryAsync(lockKey, options, scopes, reentrantAttempt, cancellationToken);
    }

    private async Task<ILockHandler> LockWithReentryAsync(string lockKey, DistributedLockAcquisitionOptions options, Dictionary<string, RedisReentrantLockContext.LocalLockScope> scopes, ValueTask<ILockHandler?> reentrantAttempt, CancellationToken cancellationToken)
    {
        try
        {
            var reentrantHandler = await reentrantAttempt.ConfigureAwait(false);
            if (reentrantHandler != null)
            {
                _logger.LogDebug("同一请求内复用锁 | LockKey={LockKey} | ReentryMode=ReuseFirstLock | SkipRedisAcquire=true", lockKey);
                return reentrantHandler;
            }

            return await LockCoreAsync(lockKey, options, scopes, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _reentrantLockContext.ClearIfEmpty(scopes);
            throw;
        }
    }

    private async Task<ILockHandler> LockCoreAsync(string lockKey, DistributedLockAcquisitionOptions options, Dictionary<string, RedisReentrantLockContext.LocalLockScope> scopes, CancellationToken cancellationToken)
    {
        var timeSettings = options.TimeModeHandler.Resolve();
        var retryStrategy = options.RetryStrategy;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "开始获取分布式锁 | LockKey={LockKey} | Expiry={Expiry} | AutoRenew={AutoRenew} | MaxRenewalCount={MaxRenewalCount}",
            lockKey,
            timeSettings.Expiry,
            timeSettings.EnableAutoRenewal,
            timeSettings.MaxRenewalCount);

        var locker = new RedisLock(_logger, _db, lockKey, timeSettings.Expiry, timeSettings.EnableAutoRenewal, timeSettings.MaxRenewalCount);
        var attempt = 0;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentAttempt = attempt + 1;
                var handler = await locker.LockAsync(currentAttempt, cancellationToken).ConfigureAwait(false);

                if (handler.Acquired)
                {
                    _logger.LogDebug(
                        "成功获取分布式锁 | LockKey={LockKey} | Attempts={Attempts} | ElapsedMs={ElapsedMs}",
                        lockKey,
                        currentAttempt,
                        stopwatch.ElapsedMilliseconds);
                    return _reentrantLockContext.Register(lockKey, handler, scopes);
                }

                attempt++;
                if (!await retryStrategy.ShouldRetryAsync(attempt, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogWarning(
                        "获取分布式锁失败且不再重试 | LockKey={LockKey} | Attempts={Attempts} | ElapsedMs={ElapsedMs}",
                        lockKey,
                        attempt,
                        stopwatch.ElapsedMilliseconds);
                    _reentrantLockContext.ClearIfEmpty(scopes);
                    return handler;
                }

                _logger.LogDebug(
                    "获取分布式锁失败，准备重试 | LockKey={LockKey} | FailedAttempts={Attempts} | ElapsedMs={ElapsedMs}",
                    lockKey,
                    attempt,
                    stopwatch.ElapsedMilliseconds);
                await retryStrategy.DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "获取分布式锁被取消 | LockKey={LockKey} | Attempts={Attempts} | ElapsedMs={ElapsedMs}",
                lockKey,
                attempt + 1,
                stopwatch.ElapsedMilliseconds);
            _reentrantLockContext.ClearIfEmpty(scopes);
            throw;
        }
        catch
        {
            _logger.LogError(
                "获取分布式锁异常退出 | LockKey={LockKey} | Attempts={Attempts} | ElapsedMs={ElapsedMs}",
                lockKey,
                attempt + 1,
                stopwatch.ElapsedMilliseconds);
            _reentrantLockContext.ClearIfEmpty(scopes);
            throw;
        }
    }
}

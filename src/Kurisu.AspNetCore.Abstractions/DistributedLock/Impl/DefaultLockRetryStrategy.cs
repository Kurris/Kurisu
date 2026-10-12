namespace Kurisu.AspNetCore.Abstractions.DistributedLock.Impl;

/// <summary>
/// 默认重试策略，按配置次数和固定间隔加随机抖动重试。
/// </summary>
public sealed class DefaultLockRetryStrategy : IDistributedLockRetryStrategy
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(100);
    private readonly int _retryCount;

    /// <summary>
    /// 创建默认重试策略
    /// </summary>
    /// <param name="retryCount">获取锁的尝试次数上限；小于或等于 0 时不重试。</param>
    public DefaultLockRetryStrategy(int retryCount = 3)
    {
        _retryCount = retryCount;
    }

    /// <inheritdoc />
    public Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        return _retryCount <= 0
            ? Task.FromResult(false)
            : Task.FromResult(attempt < _retryCount);
    }

    /// <inheritdoc />
    public Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        var baseDelay = BaseDelay.TotalMilliseconds;
        var jitterFactor = 0.8 + (Random.Shared.NextDouble() * 0.4);
        var jitteredDelay = TimeSpan.FromMilliseconds(Math.Max(1, baseDelay * jitterFactor));
        return Task.Delay(jitteredDelay, cancellationToken);
    }
}
namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 默认重试策略，按内置标准退避和次数重试。
/// </summary>
public sealed class DefaultLockRetryStrategy : AbstractDistributedLockRetryStrategy
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 创建默认重试策略
    /// </summary>
    /// <param name="retryCount">最大重试次数</param>
    public DefaultLockRetryStrategy(int retryCount = 3)
        : base(retryCount)
    {
    }

    /// <inheritdoc />
    public override Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        if (RetryCount <= 0)
            return Task.FromResult(false);

        return Task.FromResult(attempt < RetryCount);
    }

    /// <inheritdoc />
    public override Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        var baseDelay = BaseDelay.TotalMilliseconds;
        var jitterFactor = 0.8 + (Random.Shared.NextDouble() * 0.4);
        var jitteredDelay = TimeSpan.FromMilliseconds(Math.Max(1, baseDelay * jitterFactor));
        return Task.Delay(jitteredDelay, cancellationToken);
    }
}

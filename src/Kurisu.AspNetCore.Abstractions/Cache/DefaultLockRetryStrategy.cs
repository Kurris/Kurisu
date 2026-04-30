namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 默认重试策略，按配置的间隔和次数重试。
/// </summary>
public sealed class DefaultLockRetryStrategy : AbstractDistributedLockRetryStrategy
{
    /// <summary>
    /// 创建默认重试策略
    /// </summary>
    /// <param name="retryInterval">重试间隔</param>
    /// <param name="retryCount">最大重试次数</param>
    public DefaultLockRetryStrategy(TimeSpan? retryInterval = null, int retryCount = 3)
        : base(retryInterval, retryCount)
    {
    }

    /// <inheritdoc />
    public override Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        if (RetryCount <= 0 || !RetryInterval.HasValue)
            return Task.FromResult(false);

        return Task.FromResult(attempt < RetryCount);
    }
}

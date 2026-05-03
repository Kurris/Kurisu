namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 重试策略基类，提供重试次数承载与默认等待实现。
/// </summary>
public abstract class AbstractDistributedLockRetryStrategy : IDistributedLockRetryStrategy
{
    protected AbstractDistributedLockRetryStrategy(int retryCount)
    {
        RetryCount = retryCount;
    }

    /// <summary>
    /// 重试次数。
    /// </summary>
    public int RetryCount { get; }

    /// <inheritdoc />
    public abstract Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

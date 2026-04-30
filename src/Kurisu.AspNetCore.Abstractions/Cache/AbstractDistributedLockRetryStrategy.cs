namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 重试策略基类，提供构造参数承载与默认等待实现。
/// </summary>
public abstract class AbstractDistributedLockRetryStrategy : IDistributedLockRetryStrategy
{
    protected AbstractDistributedLockRetryStrategy(TimeSpan? retryInterval, int retryCount)
    {
        RetryInterval = retryInterval;
        RetryCount = retryCount;
    }

    /// <summary>
    /// 重试间隔。
    /// </summary>
    public TimeSpan? RetryInterval { get; }

    /// <summary>
    /// 重试次数。
    /// </summary>
    public int RetryCount { get; }

    /// <inheritdoc />
    public abstract Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        if (!RetryInterval.HasValue)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(RetryInterval.Value, cancellationToken);
    }
}

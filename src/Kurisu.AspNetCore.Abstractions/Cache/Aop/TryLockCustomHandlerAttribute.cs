namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 使用自定义处理器进行加锁配置。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TryLockCustomHandlerAttribute : TryLockAttribute
{
    public TryLockCustomHandlerAttribute(string scene, string tips, IDistributedLockTimeModeHandler timeModeHandler)
        : this(scene, tips, timeModeHandler, null)
    {
    }

    public TryLockCustomHandlerAttribute(string scene, string tips, IDistributedLockTimeModeHandler timeModeHandler, IDistributedLockRetryStrategy retryStrategy)
        : base(scene, tips)
    {
        TimeModeHandler = timeModeHandler;
        RetryStrategy = retryStrategy;
    }

    private IDistributedLockTimeModeHandler TimeModeHandler { get; }

    private IDistributedLockRetryStrategy RetryStrategy { get; }

    /// <inheritdoc />
    protected override IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return TimeModeHandler;
    }

    /// <inheritdoc />
    protected override IDistributedLockRetryStrategy GetRetryStrategy()
    {
        return RetryStrategy ?? base.GetRetryStrategy();
    }
}

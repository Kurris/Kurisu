namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 分布式锁获取参数。
/// </summary>
public class DistributedLockAcquisitionOptions
{
    /// <summary>
    /// 锁时间模式处理器实例。
    /// </summary>
    public IDistributedLockTimeModeHandler TimeModeHandler { get; set; }

    /// <summary>
    /// 重试策略实例。
    /// </summary>
    public IDistributedLockRetryStrategy RetryStrategy { get; set; }
}

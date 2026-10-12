namespace Kurisu.AspNetCore.Abstractions.DistributedLock.Impl;

/// <summary>
/// 默认分布式锁配置工厂。
/// </summary>
public sealed class DefaultDistributedLockOptionsFactory : IDistributedLockOptionsFactory
{
    /// <summary>
    /// 创建默认配置：6 秒过期、自动无限续期、获取锁最多尝试 3 次。
    /// 每次调用均创建新的配置及策略实例。
    /// </summary>
    public DistributedLockAcquisitionOptions CreateDefault() => new()
    {
        TimeModeHandler = new LockTimeModeHandler(),
        RetryStrategy = new DefaultLockRetryStrategy()
    };
}
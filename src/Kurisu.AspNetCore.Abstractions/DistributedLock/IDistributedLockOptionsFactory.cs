namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>
/// 创建分布式锁的默认获取配置。应用可通过依赖注入替换此工厂。
/// </summary>
public interface IDistributedLockOptionsFactory
{
    /// <summary>
    /// 创建独立的默认配置；调用方可以修改配置而不影响其他调用。
    /// </summary>
    DistributedLockAcquisitionOptions CreateDefault();
}

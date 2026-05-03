namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 可获取分布式锁的接口
/// </summary> 
/// <remarks>
/// 该接口定义了获取分布式锁的方法，允许实现类提供分布式锁的功能，以确保在分布式环境中对共享资源的访问进行协调和控制。
/// </remarks>
public interface ILockable
{
    /// <summary>
    /// 获取分布式锁
    /// </summary>
    /// <param name="lockKey">锁的Key</param>
    /// <param name="options">分布式锁的获取选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回分布式锁的处理器</returns>
    Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options, CancellationToken cancellationToken = default);

}

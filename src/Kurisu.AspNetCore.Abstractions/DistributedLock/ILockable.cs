namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

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
    /// <param name="options">分布式锁的获取选项。同一 async 调用链内对同一 key 重入时，后续调用沿用第一次成功获取时的锁模式，不会覆盖已有锁。</param>
    /// <param name="cancellationToken">取消令牌。仅用于本次获取流程；若命中本地重入，后续调用不会改变已持有锁的生命周期。</param>
    /// <returns>返回分布式锁的处理器</returns>
    Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options, CancellationToken cancellationToken = default);

}

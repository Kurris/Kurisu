namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>
/// 获取分布式锁的Key
/// </summary>
public interface ITryLockKeys
{
    /// <summary>
    /// 获取分布式锁的Key
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetKeys();
}

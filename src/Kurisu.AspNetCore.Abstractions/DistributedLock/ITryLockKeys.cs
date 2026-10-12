namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>提供业务对象对应的多个分布式锁 Key。</summary>
[Obsolete("ITryLockKeys 将在两个版本后移除，请改用 TryLockAttribute.Keys 表达式。", false)]
public interface ITryLockKeys
{
    /// <summary>获取多个锁的业务 Key。</summary>
    IEnumerable<string> GetKeys();
}
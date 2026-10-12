namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>提供业务对象对应的单个分布式锁 Key。</summary>
[Obsolete("ITryLockKey 将在两个版本后移除，请改用 TryLockAttribute.Key 表达式。", false)]
public interface ITryLockKey
{
    /// <summary>获取单个锁的业务 Key。</summary>
    string GetKey();
}
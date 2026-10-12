namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>
/// 分布式锁续期次数的特殊值；正数表示最多续期指定次数。
/// </summary>
public static class LockRenewal
{
    /// <summary>
    /// 不自动续期。
    /// </summary>
    public const int Disabled = 0;

    /// <summary>
    /// 自动无限续期。
    /// </summary>
    public const int Unlimited = -1;
}

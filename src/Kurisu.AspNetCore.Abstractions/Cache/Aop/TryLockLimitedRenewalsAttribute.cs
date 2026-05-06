namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 固定过期 + 有限续期。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TryLockLimitedRenewalsAttribute : TryLockAttribute
{
    /// <summary>
    /// 有限续期模式下的最大续期次数。
    /// </summary>
    public int MaxRenewalCount { get; }

    /// <summary>
    /// 有限续期模式必须显式指定过期时间和最大续期次数。
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="tips"></param>
    /// <param name="expirySeconds">锁过期时间，单位秒。</param>
    /// <param name="maxRenewalCount">最大续期次数。</param>
    public TryLockLimitedRenewalsAttribute(string scene, string tips, int expirySeconds, int maxRenewalCount)
        : base(scene, tips)
    {
        ExpirySeconds = expirySeconds;
        MaxRenewalCount = maxRenewalCount;
    }

    /// <inheritdoc />
    protected override IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.LimitedRenewal(GetExpiry(), MaxRenewalCount);
    }
}

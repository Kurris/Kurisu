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
    public int? MaxRenewalCount { get; set; }

    public TryLockLimitedRenewalsAttribute(string scene, string tips)
        : base(scene, tips)
    {
    }

    /// <inheritdoc />
    protected override IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.LimitedRenewal(Expiry, MaxRenewalCount ?? 3);
    }
}

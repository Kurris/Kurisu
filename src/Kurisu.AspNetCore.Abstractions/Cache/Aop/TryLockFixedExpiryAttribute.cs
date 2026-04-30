namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 固定过期，不续期。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TryLockFixedExpiryAttribute : TryLockAttribute
{
    public TryLockFixedExpiryAttribute(string scene, string tips)
        : base(scene, tips)
    {
    }

    /// <inheritdoc />
    protected override IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.FixedExpiry(Expiry);
    }
}

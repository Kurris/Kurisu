namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 固定过期，不续期。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TryLockFixedExpiryAttribute : TryLockAttribute
{
    /// <summary>
    /// 固定过期模式必须显式指定过期时间。
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="tips"></param>
    /// <param name="expirySeconds">锁过期时间，单位秒。</param>
    public TryLockFixedExpiryAttribute(string scene, string tips, int expirySeconds)
        : base(scene, tips)
    {
        ExpirySeconds = expirySeconds;
    }

    /// <inheritdoc />
    protected override IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.FixedExpiry(GetExpiry());
    }
}

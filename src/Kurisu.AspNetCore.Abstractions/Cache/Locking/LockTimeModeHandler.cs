namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 分布式锁时间模式处理器。
/// 通过静态工厂方法创建常见模式，也可直接构造自定义组合。
/// </summary>
public sealed class LockTimeModeHandler : IDistributedLockTimeModeHandler
{
    /// <summary>
    /// 锁过期时间
    /// </summary>
    public TimeSpan Expiry { get; }

    /// <summary>
    /// 是否自动续期
    /// </summary>
    public bool EnableAutoRenewal { get; }

    /// <summary>
    /// 最大续期次数，null 表示无限续期
    /// </summary>
    public int? MaxRenewalCount { get; }

    /// <summary>
    /// 创建锁时间模式处理器
    /// </summary>
    /// <param name="expiry">锁过期时间</param>
    /// <param name="enableAutoRenewal">是否自动续期</param>
    /// <param name="maxRenewalCount">最大续期次数（null=无限，仅在enableAutoRenewal=true时有意义）</param>
    public LockTimeModeHandler(TimeSpan? expiry = null, bool enableAutoRenewal = true, int? maxRenewalCount = null)
    {
        Expiry = expiry ?? TimeSpan.FromSeconds(6);
        EnableAutoRenewal = enableAutoRenewal;
        MaxRenewalCount = maxRenewalCount;

        if (Expiry <= TimeSpan.Zero)
            throw new ArgumentException("必须设置有效的过期时间", nameof(expiry));
        if (enableAutoRenewal && maxRenewalCount.HasValue && maxRenewalCount.Value <= 0)
            throw new ArgumentException("有限续期模式下最大续期次数必须大于0", nameof(maxRenewalCount));
    }

    /// <summary>
    /// 固定过期，不自动续期。
    /// </summary>
    public static LockTimeModeHandler FixedExpiry(TimeSpan? expiry = null)
        => new(expiry, enableAutoRenewal: false);

    /// <summary>
    /// 自动无限续期。
    /// </summary>
    public static LockTimeModeHandler InfiniteRenewal(TimeSpan? expiry = null)
        => new(expiry, enableAutoRenewal: true);

    /// <summary>
    /// 固定过期 + 有限次数续期。
    /// </summary>
    public static LockTimeModeHandler LimitedRenewal(TimeSpan? expiry = null, int maxRenewalCount = 3)
        => new(expiry, enableAutoRenewal: true, maxRenewalCount);

    /// <inheritdoc />
    public DistributedLockTimeSettings Resolve()
    {
        return new DistributedLockTimeSettings
        {
            Expiry = Expiry,
            EnableAutoRenewal = EnableAutoRenewal,
            MaxRenewalCount = MaxRenewalCount
        };
    }
}

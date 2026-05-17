namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 锁时间模式解析后的配置。
/// </summary>
public class DistributedLockTimeSettings
{
    /// <summary>
    /// 锁过期时间。
    /// </summary>
    public TimeSpan Expiry { get; set; }

    /// <summary>
    /// 是否自动续期。
    /// </summary>
    public bool EnableAutoRenewal { get; set; }

    /// <summary>
    /// 最大续期次数。null 表示无限续期。
    /// </summary>
    public int? MaxRenewalCount { get; set; }
}

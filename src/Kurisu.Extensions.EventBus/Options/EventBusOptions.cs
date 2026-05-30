namespace Kurisu.Extensions.EventBus.Options;

/// <summary>
/// EventBus 配置选项
/// </summary>
public class EventBusOptions
{
    /// <summary>
    /// 重试扫描间隔，默认 1 秒。
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 消息处理租约时长，超过后其他实例可重新领取。默认 5 分钟。
    /// </summary>
    public TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 每次扫描的最大消息数。默认 100。
    /// </summary>
    public int RetryBatchSize { get; set; } = 100;

    /// <summary>
    /// 最大自动重试次数，超过后转入死信。默认 5。
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// 指数退避重试的最大延迟上限。默认 1 小时。
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 终态消息保留时长，超过后自动清理。默认 7 天。设为 TimeSpan.Zero 禁用清理。
    /// </summary>
    public TimeSpan CompletedMessageRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 清理扫描间隔，默认 1 小时。
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 每次清理删除的最大行数，防止大事务锁表。默认 500。
    /// </summary>
    public int CleanupBatchSize { get; set; } = 500;
}

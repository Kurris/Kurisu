namespace Kurisu.Extensions.EventBus.Options;

/// <summary>
/// EventBus 配置选项
/// </summary>
public class EventBusOptions
{
    /// <summary>
    /// 重试兜底扫描间隔，默认 30 秒。正常发布会通过唤醒信号触发即时扫描，不依赖高频轮询。
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 消息处理租约时长，超过后其他实例可重新领取。默认 5 分钟。
    /// </summary>
    public TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 每次扫描的最大消息数。默认 100。
    /// </summary>
    public int ScanBatchSize { get; set; } = 100;

    /// <summary>
    /// 每次扫描的最大消息数兼容别名。建议使用 ScanBatchSize。
    /// </summary>
    [Obsolete("Use ScanBatchSize instead.")]
    public int RetryBatchSize
    {
        get => ScanBatchSize;
        set => ScanBatchSize = value;
    }

    /// <summary>
    /// 最大自动尝试次数，超过后转入死信。默认 5。
    /// </summary>
    public int MaxAttemptCount { get; set; } = 5;

    /// <summary>
    /// 最大自动尝试次数兼容别名。建议使用 MaxAttemptCount。
    /// </summary>
    [Obsolete("Use MaxAttemptCount instead.")]
    public int MaxRetryCount
    {
        get => MaxAttemptCount;
        set => MaxAttemptCount = value;
    }

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

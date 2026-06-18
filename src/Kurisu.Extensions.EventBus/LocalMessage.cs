using Kurisu.Extensions.SqlSugar;
using Kurisu.Extensions.SqlSugar.Attributes.DataAnnotations;

namespace Kurisu.Extensions.EventBus;

/// <summary>
/// 本地消息表
/// </summary>
[Table("LocalMessage", "本地消息表")]
public class LocalMessage : SugarEntity, IIndexConfigurator
{
    /// <summary>
    /// code
    /// </summary>
    [Column("code", false)]
    public string Code { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [Column("消息内容", false, ColumnDataType = "text")]
    public string Content { get; set; }

    /// <summary>
    /// 消息状态
    /// </summary>
    [Column("消息状态", false, IsEnum = true)]
    public LocalMessageStatus Status { get; set; } = LocalMessageStatus.Pending;

    /// <summary>
    /// 投递/处理尝试次数
    /// </summary>
    [Column("尝试次数", false)]
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// 下次重试时间（指数退避：CreateTime + 2^Attempts 分钟）
    /// </summary>
    [Column("下次重试时间", true)]
    public DateTime? NextRetryTime { get; set; }

    /// <summary>
    /// 当前处理租约到期时间
    /// </summary>
    [Column("处理租约到期时间", true)]
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// 当前处理令牌
    /// </summary>
    [Column("处理令牌", true, ColumnDataType = "varchar(36)")]
    public string ProcessingToken { get; set; }

    /// <summary>
    /// 处理结果
    /// </summary>
    [Column("处理结果", true, ColumnDataType = "text")]
    public string Result { get; set; }

    /// <summary>
    /// 转入死信的时间
    /// </summary>
    [Column("死信时间", true)]
    public DateTime? DeadLetterTime { get; set; }

    /// <summary>
    /// 人工处置原因
    /// </summary>
    [Column("人工处置原因", true, ColumnDataType = "text")]
    public string DispositionReason { get; set; }

    public List<IndexModel> GetIndexConfigs()
    {
        return [
            new IndexModel() {IsUnique = true,IndexName = "idx_local_message_code",ColumnNames = [nameof(Code)] },
            new IndexModel() {IsUnique = false,IndexName = "idx_local_message_retry",ColumnNames = [nameof(Status), nameof(NextRetryTime), nameof(LockedUntil)] }
        ];
    }
}

/// <summary>
/// 本地消息状态
/// </summary>
public enum LocalMessageStatus
{
    /// <summary>待处理，等待后台服务扫描投递</summary>
    Pending,

    /// <summary>处理中，已被领取未完成</summary>
    Processing,

    /// <summary>已完成</summary>
    Completed,

    /// <summary>死信，超过最大重试次数</summary>
    DeadLetter,

    /// <summary>人工忽略</summary>
    Ignored
}

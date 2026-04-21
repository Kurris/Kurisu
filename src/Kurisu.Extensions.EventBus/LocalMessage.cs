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
    /// 是否已处理
    /// </summary>
    [Column("是否已处理", false, IsBoolean = true)]
    public bool Processed { get; set; } = false;

    /// <summary>
    /// 重试次数
    /// </summary>
    [Column("重试次数", false)]
    public int Retry { get; set; } = 0;

    /// <summary>
    /// 下次重试时间（指数退避：CreateTime + 2^Retry 分钟）
    /// </summary>
    [Column("下次重试时间", true)]
    public DateTime? NextRetryTime { get; set; }

    /// <summary>
    /// 处理结果
    /// </summary>
    [Column("处理结果", false, ColumnDataType = "text")]
    public string Result { get; set; }

    public List<IndexModel> GetIndexConfigs()
    {
        return [
            new IndexModel() {IsUnique = true,IndexName = "idx_local_message_code",ColumnNames = [nameof(Code)] }
        ];
    }
}
using Kurisu.Core.DataAccess.Entity;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.SqlSugar.DiffLog;

/// <summary>
/// 数据变更记录
/// </summary>
[SkipScan]
[SugarTable(TableName = "TableRowChange")]
public class TableRowChangeEntity : SugarBaseEntity, ITenantId
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string RoutePath { get; set; }

    /// <summary>
    /// 操作批次
    /// </summary>
    public Guid BatchNo { get; set; }

    /// <summary>
    /// 数据操作类型
    /// </summary>
    public string Operation { get; set; }

    /// <summary>
    /// 操作表名称
    /// </summary>
    [SugarColumn(IndexGroupNameList = new[] { "idx_TableColumnChanges_TableName" })]
    public string TableName { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    public string KeyValue { get; set; }

    /// <summary>
    /// 变更字段
    /// </summary>
    [SugarColumn(IsJson = true)]
    public List<ColumnChangesDetail> Changes { get; set; }

    /// <summary>
    /// 操作的IP地址
    /// </summary>
    public string IP { get; set; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    public string CreatedByName { get; set; }


    /// <summary>
    /// 租户id
    /// </summary>
    [SugarColumn(IndexGroupNameList = new[] { "idx_TableColumnChanges_TenantId" })]
    public string TenantId { get; set; }
}

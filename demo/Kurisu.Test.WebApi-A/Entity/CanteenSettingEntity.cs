using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.DataAccess.SqlSugar;
using SqlSugar;

namespace Kurisu.Test.WebApi_A.Entity;

/// <summary>
/// 就餐配置
/// </summary>
[SugarTable(TableName = "CanteenSetting", TableDescription = "就餐配置")]
public class CanteenSettingEntity : SugarBaseEntity, ITenantId
{
    /// <summary>
    /// 唯一值
    /// </summary>
    [SugarColumn(ColumnDescription = "唯一值")]
    public string UniqueValue { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [SugarColumn(ColumnDescription = "分类")]
    public string Catalog { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    [SugarColumn(ColumnDescription = "值")]
    public string Value { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDataType = "bit", ColumnDescription = "是否启用")]
    public bool Enable { get; set; }

    public string TenantId { get; set; }
}
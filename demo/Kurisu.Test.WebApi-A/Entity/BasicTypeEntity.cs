using Kurisu.AspNetCore.DataAccess.SqlSugar;
using SqlSugar;

namespace Kurisu.Test.WebApi_A.Entity;

/// <summary>
/// Basic类型表
/// </summary>
[SugarTable(TableName = "BasicType", TableDescription = "Basic类型表")]
public class BasicTypeEntity : SugarBaseEntity
{
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnDescription = "类型")]
    public string Type { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnDescription = "编码")]
    public string Code { get; set; }

    /// <summary>
    /// 中文描述
    /// </summary>
    [SugarColumn(ColumnDescription = "中文描述")]
    public string Display { get; set; }

    /// <summary>
    /// 英文描述
    /// </summary>
    [SugarColumn(ColumnDescription = "英文描述")]
    public string DisplayEn { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注")]
    public string Remark { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序", DefaultValue = "0")]
    public int Sequence { get; set; }

}

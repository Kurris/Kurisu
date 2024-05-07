using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar;
using SqlSugar;

namespace Kurisu.Test.WebApi_A.Entity;

/// <summary>
/// 档案组
/// </summary>
[SugarTable(TableName = "GainfoGroup", TableDescription = "档案组")]
public class GainfoGroupEntity : SugarBaseEntity, ITenantId
{
    /// <summary>
    /// 父级Code
    /// </summary>
    public Guid? PCode { get; set; }

    /// <summary>
    /// 分组code
    /// </summary>
    [SugarColumn(ColumnDescription = "分组code")]
    public Guid Code { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称")]
    public string Name { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注")]
    public string Remark { get; set; }

    /// <summary>
    /// 是否默认分组
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认分组", ColumnDataType = "bit", DefaultValue = "1")]
    public bool IsDefault { get; set; } = true;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    [SugarColumn(ColumnDescription = "是否可编辑", ColumnDataType = "bit", DefaultValue = "1")]
    public bool Editable { get; set; } = true;


    public string TenantId { get; set; }
}

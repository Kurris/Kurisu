using System;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar;

/// <summary>
/// SqlSugar Base实体
/// </summary>
public class SugarBaseEntity : BaseEntity<long, string>
{
    /// <summary>
    /// 主键id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public override long Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [InsertUserGeneration]
    public override string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IndexGroupNameList = ["idx_CreateTime"])]
    [InsertDateTimeGeneration]

    public override DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    [UpdateUserGeneration]
    [InsertUserGeneration]
    public override string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// 修改时间
    /// </summary>
    [UpdateDateTimeGeneration]
    [InsertDateTimeGeneration]
    public override DateTime ModifiedTime { get; set; }
}
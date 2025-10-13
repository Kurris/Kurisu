using System;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar;

/// <summary>
/// SqlSugar Base实体
/// </summary>
public class SugarBaseEntity : BaseEntity<long, int>
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
    public override int CreatedBy { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public override string CreatedByName { get; set; }

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
    public override int ModifiedBy { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public override string ModifiedByName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [UpdateDateTimeGeneration]
    [InsertDateTimeGeneration]
    public override DateTime ModifiedTime { get; set; }
}
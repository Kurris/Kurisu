using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.DataAccess.SqlSugar;
using SqlSugar;

namespace Dlhis.Entity.Basic.Entity;

/// <summary>
/// 档案人脸信息
/// </summary>
[SkipScan]
[SugarTable(TableName = "GainfoFace", TableDescription = "档案人脸信息")]
public class GainfoFaceEntity : SugarBaseEntity
{

    /// <summary>
    /// 学工号
    /// </summary>
    [SugarColumn(IsNullable = false, UniqueGroupNameList = new[] { "idx_GainfoFace_UserCode" })]
    public string UserCode { get; set; }


    /// <summary>
    /// 人脸
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = false)]
    public string Face { get; set; }
}

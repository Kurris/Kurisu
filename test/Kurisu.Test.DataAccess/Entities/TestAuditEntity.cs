using Kurisu.Extensions.SqlSugar;
using Kurisu.Extensions.SqlSugar.Attributes;
using SqlSugar;

namespace Kurisu.Test.DataAccess.Entities;

/// <summary>
/// 审计字段测试实体，验证 InsertDateTimeGeneration / UpdateDateTimeGeneration 自动填充
/// </summary>
[SugarTable("test_audit")]
public class TestAuditEntity : SugarEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public new int Id { get; set; }

    public string Name { get; set; }

    /// <summary>创建时间——插入时自动填充</summary>
    [InsertDateTimeGeneration]
    public DateTime CreatedTime { get; set; }

    /// <summary>更新时间——更新时自动填充</summary>
    [UpdateDateTimeGeneration]
    public DateTime UpdatedTime { get; set; }
}

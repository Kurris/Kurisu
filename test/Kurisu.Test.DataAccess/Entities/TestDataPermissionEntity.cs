using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.Extensions.SqlSugar;
using SqlSugar;

namespace Kurisu.Test.DataAccess.Entities;

/// <summary>
/// 数据权限测试实体，包含 DepartmentId 用于验证数据权限过滤
/// </summary>
[SugarTable("test_data_permission")]
public class TestDataPermissionEntity : SugarEntity, ITenantId
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public new int Id { get; set; }

    public string Name { get; set; }

    /// <summary>部门ID，用于数据权限过滤</summary>
    public string DepartmentId { get; set; }

    /// <summary>可空权限字段，用于验证 Nullable 值类型过滤。</summary>
    public Guid? OptionalDepartmentId { get; set; }

    /// <inheritdoc />
    public string TenantId { get; set; }
}

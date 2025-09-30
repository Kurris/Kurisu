using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar;
using SqlSugar;

namespace Kurisu.Test.DataAccess.Entities;

[SugarTable("test1")]
public class Test1Entity : SugarBaseEntity , ITenantId
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public new int Id { get; set; }

    public string Name { get; set; }
    public string Type { get; set; }
    public int Age { get; set; }

    /// <inheritdoc />
    public string TenantId { get; set; }
}

[SugarTable("test1WithSoftDelete")]
public class Test1WithSoftDeleteEntity : SugarBaseEntity, ISoftDeleted
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public new int Id { get; set; }

    public string Name { get; set; }
    public string Type { get; set; }
    public int Age { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }
}
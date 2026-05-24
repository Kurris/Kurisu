namespace Kurisu.Extensions.SqlSugar.Sharding;

/// <summary>
/// 标记实体启用分表查询，需同时实现 <see cref="Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field.ITenantId"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EnableShardingAttribute : Attribute
{
}

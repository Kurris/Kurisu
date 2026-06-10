namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 空数据库租户上下文访问器。
/// </summary>
public class NullDbTenantAccessor : IDbTenantAccessor
{
    public bool HasTenant => false;

    public string GetTenantId()
    {
        return null;
    }

    public IReadOnlyList<string> GetAccessibleTenantIds()
    {
        return [];
    }
}

using Kurisu.AspNetCore.Abstractions.Authentication;

namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 基于 ICurrentUser 的数据库租户上下文访问器。
/// </summary>
public class CurrentUserDbTenantAccessor(ICurrentUser currentUser) : IDbTenantAccessor
{
    public bool HasTenant => !string.IsNullOrEmpty(GetTenantId());

    public string GetTenantId()
    {
        return currentUser.GetTenantId();
    }

    public IReadOnlyList<string> GetAccessibleTenantIds()
    {
        var tenants = currentUser.GetUserClaim("tenants");
        if (string.IsNullOrWhiteSpace(tenants))
        {
            return [];
        }

        return tenants
            .Split(',')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
    }
}

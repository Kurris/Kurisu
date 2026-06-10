using Kurisu.AspNetCore.Abstractions.Authentication;

namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 基于 ICurrentUser 的数据库审计用户访问器。
/// </summary>
public class CurrentUserDbAuditAccessor(ICurrentUser currentUser) : IDbAuditAccessor
{
    public object GetUserId()
    {
        return currentUser.GetUserId();
    }

    public string GetUserName()
    {
        return currentUser.GetName();
    }
}

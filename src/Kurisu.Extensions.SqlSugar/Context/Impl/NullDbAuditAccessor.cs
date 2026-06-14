namespace Kurisu.Extensions.SqlSugar.Context.Impl;

/// <summary>
/// 空数据库审计用户访问器。
/// </summary>
public class NullDbAuditAccessor : IDbAuditAccessor
{
    public object GetUserId()
    {
        return -1;
    }

    public string GetUserName()
    {
        return "system";
    }
}

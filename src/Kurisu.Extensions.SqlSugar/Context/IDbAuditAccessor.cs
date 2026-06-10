namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 数据库审计用户访问器。
/// </summary>
public interface IDbAuditAccessor
{
    /// <summary>
    /// 获取当前审计用户ID。
    /// </summary>
    object GetUserId();

    /// <summary>
    /// 获取当前审计用户名。
    /// </summary>
    string GetUserName();
}

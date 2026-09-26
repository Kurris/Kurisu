namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

/// <summary>
/// 数据库租户上下文访问器。
/// 业务层可直接注入此接口获取当前有效租户ID，
/// 实现应优先返回当前租户作用域的覆盖值，
/// 其次通过用户身份获取。
/// 若返回 null 则表示未解析到租户，应使用 [UseTenant] 或开启接口授权。
/// </summary>
public interface IDbTenantAccessor
{
    /// <summary>
    /// 获取当前有效租户ID。
    /// 优先返回 UseTenant 作用域覆盖值，其次返回用户身份租户。
    /// 若返回 null 则表示未解析到租户。
    /// </summary>
    string GetTenantId();

    /// <summary>
    /// 获取允许访问的租户ID集合（基于用户身份）。
    /// </summary>
    IReadOnlyList<string> GetAccessibleTenantIds();
}

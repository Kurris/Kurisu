namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 数据库租户上下文访问器。
/// </summary>
public interface IDbTenantAccessor
{
    /// <summary>
    /// 是否存在当前租户。
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// 获取当前租户ID。
    /// </summary>
    string GetTenantId();

    /// <summary>
    /// 获取允许访问的租户ID集合。
    /// </summary>
    IReadOnlyList<string> GetAccessibleTenantIds();
}

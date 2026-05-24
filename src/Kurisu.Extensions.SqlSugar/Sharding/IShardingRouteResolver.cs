namespace Kurisu.Extensions.SqlSugar.Sharding;

/// <summary>
/// 分表路由解析器，根据租户ID解析分表后缀
/// </summary>
public interface IShardingRouteResolver
{
    /// <summary>
    /// 根据租户ID获取分表后缀
    /// </summary>
    string GetSuffix(string tenantId);
}

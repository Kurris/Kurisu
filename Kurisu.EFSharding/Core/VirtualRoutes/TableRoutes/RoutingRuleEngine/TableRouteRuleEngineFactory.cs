using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

/// <summary>
/// 表路由规则引擎工厂
/// </summary>
public class TableRouteRuleEngineFactory : ITableRouteRuleEngineFactory
{
    private readonly ITableRouteRuleEngine _tableRouteRuleEngine;

    public TableRouteRuleEngineFactory(ITableRouteRuleEngine tableRouteRuleEngine)
    {
        _tableRouteRuleEngine = tableRouteRuleEngine;
    }
    /// <summary>
    /// 创建表路由上下文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <returns></returns>
    private TableRouteRuleContext CreateContext(DatasourceRouteResult datasourceRouteResult, IQueryable queryable, Dictionary<Type, IQueryable> queryEntities)
    {
        return new TableRouteRuleContext(datasourceRouteResult,queryable,queryEntities);
    }
    public ShardingRouteResult Route(DatasourceRouteResult dataSourceRouteResult, IQueryable queryable, Dictionary<Type, IQueryable> queryEntities)
    {
        var ruleContext = CreateContext(dataSourceRouteResult,queryable, queryEntities);
        return Route(ruleContext);
    }

    private ShardingRouteResult Route(TableRouteRuleContext ruleContext)
    {
        return _tableRouteRuleEngine.Route(ruleContext);
    }
}
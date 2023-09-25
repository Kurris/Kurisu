using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

public interface ITableRouteRuleEngineFactory
{
    ShardingRouteResult Route(DatasourceRouteResult datasourceRouteResult, IQueryable queryable, Dictionary<Type, IQueryable> queryEntities);
}
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.datasourceRoutes.RouteRuleEngine;

public interface IDatasourceRouteRuleEngineFactory
{
    DatasourceRouteResult Route(IQueryable queryable, IShardingDbContext shardingDbContext, Dictionary<Type, IQueryable> queryEntities);
}
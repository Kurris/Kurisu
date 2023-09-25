using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.datasourceRoutes.RouteRuleEngine;

public class DatasourceRouteRuleEngine : IDatasourceRouteRuleEngine
{
    private readonly IMetadataManager _entityMetadataManager;
    private readonly IVirtualDatasource _virtualDatasource;
    private readonly IDatasourceRouteManager _datasourceRouteManager;

    public DatasourceRouteRuleEngine(IMetadataManager entityMetadataManager, IVirtualDatasource virtualDatasource, IDatasourceRouteManager datasourceRouteManager)
    {
        _entityMetadataManager = entityMetadataManager;
        _virtualDatasource = virtualDatasource;
        _datasourceRouteManager = datasourceRouteManager;
    }

    public DatasourceRouteResult Route(DatasourceRouteRuleContext routeRuleContext)
    {
        var datasourceMaps = new Dictionary<Type, ISet<string>>();

        foreach (var queryEntityKv in routeRuleContext.QueryEntities)
        {
            var queryEntity = queryEntityKv.Key;
            if (!_entityMetadataManager.IsShardingDatasource(queryEntity))
            {
                datasourceMaps.Add(queryEntity, new HashSet<string>() {_virtualDatasource.DefaultDatasourceName});
                continue;
            }

            var datasourceConfigs = _datasourceRouteManager.RouteTo(queryEntity, new ShardingDatasourceRouteConfig(queryEntityKv.Value ?? routeRuleContext.Queryable));
            if (!datasourceMaps.ContainsKey(queryEntity))
            {
                datasourceMaps.Add(queryEntity, datasourceConfigs.ToHashSet());
            }
            else
            {
                foreach (var shardingDatasource in datasourceConfigs)
                {
                    datasourceMaps[queryEntity].Add(shardingDatasource);
                }
            }
        }

        if (datasourceMaps.IsEmpty())
            throw new ShardingCoreException(
                $"data source route not match: {routeRuleContext.Queryable.ShardingPrint()}");
        if (datasourceMaps.Count == 1)
            return new DatasourceRouteResult(datasourceMaps.First().Value);
        var intersect = datasourceMaps.Select(o => o.Value).Aggregate((p, n) => p.Intersect(n).ToHashSet());
        return new DatasourceRouteResult(intersect);
    }
}
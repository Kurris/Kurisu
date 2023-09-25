using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

public class TableRouteRuleEngine : ITableRouteRuleEngine
{
    private readonly ITableRouteManager _tableRouteManager;
    private readonly IMetadataManager _entityMetadataManager;
    private readonly IParallelTableManager _parallelTableManager;

    public TableRouteRuleEngine(ITableRouteManager tableRouteManager,
        IMetadataManager entityMetadataManager, IParallelTableManager parallelTableManager)
    {
        _tableRouteManager = tableRouteManager;
        _entityMetadataManager = entityMetadataManager;
        _parallelTableManager = parallelTableManager;
    }

    private List<TableRouteUnit> GetEntityRouteUnit(DatasourceRouteResult dataSourceRouteResult,
        Type shardingEntity, IQueryable queryable)
    {
        var virtualTableRoute = _tableRouteManager.GetRoute(shardingEntity);
        return virtualTableRoute.RouteWithPredicate(dataSourceRouteResult, queryable, true);
    }

    public ShardingRouteResult Route(TableRouteRuleContext tableRouteRuleContext)
    {
        var routeMaps = new Dictionary<string, Dictionary<Type, ISet<TableRouteUnit>>>();
        var queryEntities = tableRouteRuleContext.QueryEntities;


        var onlyShardingDataSource = queryEntities.All(o => _entityMetadataManager.IsShardingDatasourceOnly(o.Key));
        foreach (var (shardingEntity, value) in queryEntities)
        {
            if (!_entityMetadataManager.IsShardingTable(shardingEntity))
            {
                continue;
            }


            var shardingRouteUnits = GetEntityRouteUnit(tableRouteRuleContext.DatasourceRouteResult, shardingEntity,
                value ?? tableRouteRuleContext.Queryable);

            foreach (var shardingRouteUnit in shardingRouteUnits)
            {
                var dataSourceName = shardingRouteUnit.DataSourceName;


                if (!routeMaps.ContainsKey(dataSourceName))
                {
                    routeMaps.Add(dataSourceName,
                        new Dictionary<Type, ISet<TableRouteUnit>>()
                            {{shardingEntity, new HashSet<TableRouteUnit>() {shardingRouteUnit}}});
                }
                else
                {
                    var routeMap = routeMaps[dataSourceName];
                    if (!routeMap.ContainsKey(shardingEntity))
                    {
                        routeMap.Add(shardingEntity, new HashSet<TableRouteUnit>() {shardingRouteUnit});
                    }
                    else
                    {
                        routeMap[shardingEntity].Add(shardingRouteUnit);
                    }
                }
            }
        }


        var sqlRouteUnits = new List<ISqlRouteUnit>(31);
        var isCrossTable = false;
        var existCrossTableTails = false;
        var dataSourceSet = new HashSet<string>();
        foreach (var datasourceName in tableRouteRuleContext.DatasourceRouteResult.IntersectDataSources)
        {
            if (routeMaps.ContainsKey(datasourceName))
            {
                var routeMap = routeMaps[datasourceName];
                var routeResults = routeMap.Select(o => o.Value).Cartesian()
                    .Select(o => new TableRouteResult(o.ToList())).Where(o => !o.IsEmpty).ToArray();

                //平行表
                var tableRouteResults = GetTableRouteResults(tableRouteRuleContext, routeResults);
                if (tableRouteResults.IsNotEmpty())
                {
                    dataSourceSet.Add(datasourceName);
                    if (tableRouteResults.Length > 1)
                    {
                        isCrossTable = true;
                    }

                    foreach (var tableRouteResult in tableRouteResults)
                    {
                        if (tableRouteResult.ReplaceTables.Count > 1)
                        {
                            isCrossTable = true;
                            if (tableRouteResult.HasDifferentTail)
                            {
                                existCrossTableTails = true;
                            }
                        }

                        sqlRouteUnits.Add(new SqlRouteUnit(datasourceName, tableRouteResult));
                    }
                }
            }
            else if (onlyShardingDataSource)
            {
                dataSourceSet.Add(datasourceName);
                var tableRouteResult = new TableRouteResult(queryEntities.Keys.Select(o => new TableRouteUnit(o, datasourceName, string.Empty)).ToList());
                sqlRouteUnits.Add(new SqlRouteUnit(datasourceName, tableRouteResult));
            }
        }

        return new ShardingRouteResult(sqlRouteUnits, sqlRouteUnits.Count == 0, dataSourceSet.Count > 1, isCrossTable,
            existCrossTableTails);
    }

    private TableRouteResult[] GetTableRouteResults(TableRouteRuleContext tableRouteRuleContext, TableRouteResult[] routeResults)
    {
        if (tableRouteRuleContext.QueryEntities.Count > 1 && routeResults.Length > 0)
        {
            var queryShardingTables = tableRouteRuleContext.QueryEntities.Keys.Where(o => _entityMetadataManager.IsShardingTable(o)).ToArray();
            if (queryShardingTables.Length > 1 && _parallelTableManager.IsParallelTableQuery(queryShardingTables))
            {
                return routeResults.Where(o => !o.HasDifferentTail).ToArray();
            }
        }

        return routeResults;
    }
}
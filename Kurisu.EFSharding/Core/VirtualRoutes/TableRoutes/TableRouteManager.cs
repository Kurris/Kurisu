using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.ShardingEnumerableQueries;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;

public class TableRouteManager : ITableRouteManager
{
    private readonly IVirtualDatasource _virtualDatasource;
    private readonly ConcurrentDictionary<Type, IVirtualTableRoute> _tableRoutes = new();

    public TableRouteManager(IVirtualDatasource virtualdatasource)
    {
        _virtualDatasource = virtualdatasource;
    }

    public bool HasRoute(Type entityType)
    {
        return _tableRoutes.ContainsKey(entityType);
    }

    public IVirtualTableRoute GetRoute(Type entityType)
    {
        if (!_tableRoutes.TryGetValue(entityType, out var tableRoute))
            throw new ShardingCoreInvalidOperationException(
                $"entity type :[{entityType.FullName}] not found table route");
        return tableRoute;
    }

    public List<IVirtualTableRoute> GetRoutes()
    {
        return _tableRoutes.Values.ToList();
    }

    public bool AddRoute(IVirtualTableRoute route)
    {
        if (!route.Metadata.IsTableMetadata)
            throw new ShardingCoreInvalidOperationException(
                $"{route.Metadata.ClrType.FullName} should configure sharding table");

        return _tableRoutes.TryAdd(route.Metadata.ClrType, route);
    }

    public List<TableRouteUnit> RouteTo(Type entityType, ShardingTableRouteConfig shardingTableRouteConfig)
    {
        return RouteTo(entityType, _virtualDatasource.DefaultDatasourceName, shardingTableRouteConfig);
    }

    public List<TableRouteUnit> RouteTo(Type entityType, string datasourceName, ShardingTableRouteConfig shardingTableRouteConfig)
    {
        var datasourceRouteResult = new DatasourceRouteResult(_virtualDatasource.DefaultDatasourceName);
        return RouteTo(entityType, datasourceRouteResult, shardingTableRouteConfig);
    }

    public List<TableRouteUnit> RouteTo(Type entityType, DatasourceRouteResult datasourceRouteResult,
        ShardingTableRouteConfig tableRouteConfig)
    {
        var route = GetRoute(entityType);
        if (tableRouteConfig.UseQueryable())
            return route.RouteWithPredicate(datasourceRouteResult, tableRouteConfig.GetQueryable(), true);
        if (tableRouteConfig.UsePredicate())
        {
            var shardingEmptyEnumerableQuery = (IShardingEmptyEnumerableQuery) Activator.CreateInstance(
                typeof(ShardingEmptyEnumerableQuery<>).GetGenericType0(entityType),
                tableRouteConfig.GetPredicate());

            return route.RouteWithPredicate(datasourceRouteResult, shardingEmptyEnumerableQuery!.EmptyQueryable(),
                false);
        }

        object shardingKeyValue = null;
        if (tableRouteConfig.UseValue())
            shardingKeyValue = tableRouteConfig.GetShardingKeyValue();

        if (tableRouteConfig.UseEntity())
            shardingKeyValue = tableRouteConfig.GetShardingEntity()
                .GetPropertyValue(route.Metadata.Property.Name);

        if (shardingKeyValue == null)
            throw new ShardingCoreException(" route entity queryable or sharding key value is null ");
        var shardingRouteUnit = route.RouteWithValue(datasourceRouteResult, shardingKeyValue);
        return new List<TableRouteUnit>(1) {shardingRouteUnit};
    }
}
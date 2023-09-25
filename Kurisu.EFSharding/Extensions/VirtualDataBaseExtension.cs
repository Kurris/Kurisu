using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;

namespace Kurisu.EFSharding.Extensions;

public static class VirtualDataBaseExtension
{
    public static string GetTableTail<TEntity>(this ITableRouteManager tableRouteManager, string dataSourceName,
        TEntity entity, Type realEntityType) where TEntity : class
    {
        var shardingRouteUnit = tableRouteManager.RouteTo(realEntityType, dataSourceName, new ShardingTableRouteConfig(shardingTable: entity))[0];
        return shardingRouteUnit.Tail;
    }

    public static string GetTableTail<TEntity>(this ITableRouteManager tableRouteManager, string dataSourceName,
        object shardingKeyValue, Type realEntityType) where TEntity : class
    {
        var shardingRouteUnit = tableRouteManager.RouteTo(realEntityType, dataSourceName, new ShardingTableRouteConfig(shardingKeyValue: shardingKeyValue))[0];
        return shardingRouteUnit.Tail;
    }

    public static bool IsVirtualDatasourceRoute(this Type routeType)
    {
        if (routeType == null)
            throw new ArgumentNullException(nameof(routeType));
        return typeof(IVirtualDatasourceRoute).IsAssignableFrom(routeType);
    }

    public static bool IsIVirtualTableRoute(this Type routeType)
    {
        if (routeType == null)
            throw new ArgumentNullException(nameof(routeType));
        return typeof(IVirtualTableRoute).IsAssignableFrom(routeType);
    }
}
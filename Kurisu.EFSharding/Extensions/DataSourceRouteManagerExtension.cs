using System.Linq.Expressions;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;

namespace Kurisu.EFSharding.Extensions;

public static class DatasourceRouteManagerExtension
{
    public static string GetDatasourceName<TEntity>(this IDatasourceRouteManager datasourceRouteManager, TEntity entity, Type realEntityType) where TEntity : class
    {
        return datasourceRouteManager.RouteTo(realEntityType,
            new ShardingDatasourceRouteConfig(shardingDatasource: entity))[0];
    }

    public static List<string> GetDatasourceNames<TEntity>(this IDatasourceRouteManager datasourceRouteManager, Expression<Func<TEntity, bool>> where)
        where TEntity : class
    {
        return datasourceRouteManager.RouteTo(typeof(TEntity), new ShardingDatasourceRouteConfig(predicate: where))
            .ToList();
    }

    public static string GetDatasourceName<TEntity>(this IDatasourceRouteManager datasourceRouteManager, object shardingKeyValue) where TEntity : class
    {
        return datasourceRouteManager.RouteTo(typeof(TEntity),
            new ShardingDatasourceRouteConfig(shardingKeyValue: shardingKeyValue))[0];
    }
}
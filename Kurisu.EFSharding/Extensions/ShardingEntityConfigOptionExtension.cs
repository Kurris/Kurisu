using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingEntityConfigOptionExtension
{
    public static bool TryGetVirtualTableRoute<TEntity>(this IRouteOptions shardingRouteConfigOptions, out Type virtualTableRouteType) where TEntity : class
    {
        if (shardingRouteConfigOptions.HasVirtualTableRoute(typeof(TEntity)))
        {
            virtualTableRouteType = shardingRouteConfigOptions.GetVirtualTableRouteType(typeof(TEntity));
            return virtualTableRouteType != null;
        }

        virtualTableRouteType = null;
        return false;
    }

    public static bool TryGetVirtualDatasourceRoute<TEntity>(this IRouteOptions shardingRouteConfigOptions, out Type virtualDatasourceRouteType) where TEntity : class
    {
        if (shardingRouteConfigOptions.HasVirtualDatasourceRoute(typeof(TEntity)))
        {
            virtualDatasourceRouteType = shardingRouteConfigOptions.GetVirtualDatasourceRouteType(typeof(TEntity));
            return virtualDatasourceRouteType != null;
        }

        virtualDatasourceRouteType = null;
        return false;
    }
}
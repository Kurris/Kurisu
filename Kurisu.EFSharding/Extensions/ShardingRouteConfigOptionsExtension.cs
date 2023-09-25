using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingRouteConfigOptionsExtension
{

    /// <summary>
    /// 添加分表路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    public static void AddShardingDatasourceRoute<TRoute>(this IRouteOptions options) where TRoute : IVirtualDatasourceRoute
    {
        var routeType = typeof(TRoute);
        options.AddShardingDatasourceRoute(routeType);
    }
    /// <summary>
    /// 添加分表路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    public static void AddShardingTableRoute<TRoute>(this IRouteOptions options) where TRoute : IVirtualTableRoute
    {
        var routeType = typeof(TRoute);
        options.AddShardingTableRoute(routeType);
    }
}
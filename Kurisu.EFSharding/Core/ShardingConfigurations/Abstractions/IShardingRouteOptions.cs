using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;

namespace Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;

/// <summary>
/// 分片路由配置
/// </summary>
public interface IRouteOptions
{
    /// <summary>
    /// 获取所有的分表路由类型
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type> TableRouteTypes { get; }

    /// <summary>
    /// 获取所有的分库路由类型
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type> DatasourceRouteTypes { get; }

    /// <summary>
    /// 所有路由类型
    /// </summary>
    public HashSet<Type> RouteTypes { get; }

    /// <summary>
    /// 添加分库路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    void AddShardingDatasourceRoute<TRoute>();

    /// <summary>
    /// 添加分库路由
    /// </summary>
    /// <param name="routeType"></param>
    void AddShardingDatasourceRoute(Type routeType);

    /// <summary>
    /// 添加分表路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    void AddShardingTableRoute<TRoute>() where TRoute : IVirtualTableRoute;

    /// <summary>
    /// 添加分表路由
    /// </summary>
    /// <param name="routeType"></param>
    void AddShardingTableRoute(Type routeType);

    /// <summary>
    /// 是否有虚拟表路由
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool HasVirtualTableRoute(Type entityType);

    /// <summary>
    /// 获取虚拟表路由
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    Type GetVirtualTableRouteType(Type entityType);

    /// <summary>
    /// 是否有虚拟库路由
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool HasVirtualDatasourceRoute(Type entityType);

    /// <summary>
    /// 获取虚拟库路由
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    Type GetVirtualDatasourceRouteType(Type entityType);
}
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.ShardingConfigurations;

public class ShardingRouteConfigOptions : IRouteOptions
{
    private readonly IDictionary<Type, Type> _virtualDatasourceRoutes = new Dictionary<Type, Type>();
    private readonly IDictionary<Type, Type> _virtualTableRoutes = new Dictionary<Type, Type>();


    /// <summary>
    /// 添加分库路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    public void AddShardingDatasourceRoute<TRoute>()
    {
        var routeType = typeof(TRoute);
        AddShardingDatasourceRoute(routeType);
    }

    public void AddShardingDatasourceRoute(Type routeType)
    {
        if (!routeType.IsVirtualDatasourceRoute())
            throw new ShardingCoreInvalidOperationException(routeType.FullName);
        //获取类型
        var genericVirtualRoute = routeType.GetInterfaces().FirstOrDefault(it => it.IsInterface && it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IVirtualDatasourceRoute<>)
                                                                                 && it.GetGenericArguments().Any());
        if (genericVirtualRoute == null)
            throw new ArgumentException($"add sharding route type error not assignable from {nameof(IVirtualDatasourceRoute<object>)}.");

        var shardingEntityType = genericVirtualRoute.GetGenericArguments()[0];
        if (shardingEntityType == null)
            throw new ArgumentException($"add sharding table route type error not assignable from {nameof(IVirtualDatasourceRoute<object>)}.");
        if (!_virtualDatasourceRoutes.ContainsKey(shardingEntityType))
        {
            _virtualDatasourceRoutes.Add(shardingEntityType, routeType);
        }
    }

    /// <summary>
    /// 添加分表路由
    /// </summary>
    /// <typeparam name="TRoute"></typeparam>
    public void AddShardingTableRoute<TRoute>() where TRoute : IVirtualTableRoute
    {
        var routeType = typeof(TRoute);
        AddShardingTableRoute(routeType);
    }

    public void AddShardingTableRoute(Type routeType)
    {
        if (!routeType.IsIVirtualTableRoute())
            throw new ShardingCoreInvalidOperationException(routeType.FullName);
        //获取类型
        var genericVirtualRoute = routeType.GetInterfaces().FirstOrDefault(it => it.IsInterface && it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IVirtualTableRoute<>)
                                                                                 && it.GetGenericArguments().Any());
        if (genericVirtualRoute == null)
            throw new ArgumentException($"add sharding route type error not assignable from {nameof(IVirtualTableRoute<object>)}.");

        var shardingEntityType = genericVirtualRoute.GetGenericArguments()[0];
        if (shardingEntityType == null)
            throw new ArgumentException($"add sharding table route type error not assignable from {nameof(IVirtualTableRoute<object>)}.");
        if (!_virtualTableRoutes.ContainsKey(shardingEntityType))
        {
            _virtualTableRoutes.Add(shardingEntityType, routeType);
        }
    }

    public bool HasVirtualTableRoute(Type entityType)
    {
        return _virtualTableRoutes.ContainsKey(entityType);
    }

    public Type GetVirtualTableRouteType(Type entityType)
    {
        if (!_virtualTableRoutes.ContainsKey(entityType))
            throw new ArgumentException($"{entityType} not found {nameof(IVirtualTableRoute)}");
        return _virtualTableRoutes[entityType];
    }

    public bool HasVirtualDatasourceRoute(Type entityType)
    {
        return _virtualDatasourceRoutes.ContainsKey(entityType);
    }

    public Type GetVirtualDatasourceRouteType(Type entityType)
    {
        if (!_virtualDatasourceRoutes.ContainsKey(entityType))
            throw new ArgumentException($"{entityType} not found {nameof(IVirtualDatasourceRoute)}");
        return _virtualDatasourceRoutes[entityType];
    }

    public IEnumerable<Type> TableRouteTypes => _virtualTableRoutes.Keys;

    public IEnumerable<Type> DatasourceRouteTypes => _virtualDatasourceRoutes.Keys.ToHashSet();

    public HashSet<Type> RouteTypes => TableRouteTypes.Concat(DatasourceRouteTypes).ToHashSet();
}
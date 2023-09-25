using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.ShardingEnumerableQueries;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;

public class DatasourceRouteManager : IDatasourceRouteManager
{
    private readonly IMetadataManager _metadataManager;
    private readonly IVirtualDatasource _virtualDatasource;
    private readonly ConcurrentDictionary<Type, IVirtualDatasourceRoute> _datasourceVirtualRoutes = new();

    public DatasourceRouteManager(IMetadataManager entityMetadataManager, IVirtualDatasource virtualDatasource)
    {
        _metadataManager = entityMetadataManager;
        _virtualDatasource = virtualDatasource;
    }

    public bool HasRoute(Type entityType)
    {
        return _datasourceVirtualRoutes.ContainsKey(entityType);
    }

    public List<string> RouteTo(Type entityType, ShardingDatasourceRouteConfig routeRouteConfig)
    {
        if (!_metadataManager.IsShardingDatasource(entityType))
            return new List<string>(1) {_virtualDatasource.DefaultDatasourceName};
        var virtualDatasourceRoute = GetRoute(entityType);

        if (routeRouteConfig.UseQueryable())
            return virtualDatasourceRoute.RouteWithPredicate(routeRouteConfig.GetQueryable(), true);
        if (routeRouteConfig.UsePredicate())
        {
            var shardingEmptyEnumerableQuery = (IShardingEmptyEnumerableQuery) Activator.CreateInstance(typeof(ShardingEmptyEnumerableQuery<>).MakeGenericType(entityType), routeRouteConfig.GetPredicate());
            return virtualDatasourceRoute.RouteWithPredicate(shardingEmptyEnumerableQuery.EmptyQueryable(), false);
        }

        object shardingKeyValue = null;
        if (routeRouteConfig.UseValue())
            shardingKeyValue = routeRouteConfig.GetShardingKeyValue();

        if (routeRouteConfig.UseEntity())
        {
            shardingKeyValue = routeRouteConfig.GetShardingdatasource().GetPropertyValue(virtualDatasourceRoute.Metadata.Property.Name);
        }

        if (shardingKeyValue != null)
        {
            var datasourceName = virtualDatasourceRoute.RouteWithValue(shardingKeyValue);
            return new List<string>(1) {datasourceName};
        }

        throw new NotImplementedException(nameof(ShardingDatasourceRouteConfig));
    }

    public IVirtualDatasourceRoute GetRoute(Type entityType)
    {
        if (!_datasourceVirtualRoutes.TryGetValue(entityType, out var datasourceVirtualRoute))
            throw new ShardingCoreInvalidOperationException(
                $"entity type :[{entityType.FullName}] not found virtual data source route");
        return datasourceVirtualRoute;
    }


    /// <summary>
    /// 添加分库路由
    /// </summary>
    /// <param name="virtualDatasourceRoute"></param>
    /// <returns></returns>
    /// <exception cref="ShardingCoreInvalidOperationException">对象未配置分库</exception>
    public bool AddDatasourceRoute(IVirtualDatasourceRoute virtualDatasourceRoute)
    {
        if (!virtualDatasourceRoute.Metadata.IsDatasourceMetadata)
            throw new ShardingCoreInvalidOperationException($"{virtualDatasourceRoute.Metadata.ClrType.FullName} should configure sharding data source");

        return _datasourceVirtualRoutes.TryAdd(virtualDatasourceRoute.Metadata.ClrType, virtualDatasourceRoute);
    }
}
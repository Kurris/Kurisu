using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Core.Metadata.Builder;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Bootstrap;

/// <summary>
/// 对象元数据初始化器
/// </summary>
/// <typeparam name="TEntity"></typeparam>
internal class MetadataInitializer<TEntity> : IMetadataInitializer
    where TEntity : class, new()
{
    private readonly IShardingProvider _shardingProvider;
    private readonly IRouteOptions _routeOptions;
    private readonly IDatasourceRouteManager _dataSourceRouteManager;
    private readonly ITableRouteManager _tableRouteManager;
    private readonly IMetadataManager _metadataManager;

    public MetadataInitializer(
        IShardingProvider shardingProvider,
        IRouteOptions shardingRouteConfigOptions,
        IDatasourceRouteManager dataSourceRouteManager,
        ITableRouteManager tableRouteManager,
        IMetadataManager entityMetadataManager
    )
    {
        _shardingProvider = shardingProvider;
        _routeOptions = shardingRouteConfigOptions;
        _dataSourceRouteManager = dataSourceRouteManager;
        _tableRouteManager = tableRouteManager;
        _metadataManager = entityMetadataManager;
    }

    /// <summary>
    /// 初始化
    /// 针对对象在dbcontext中的主键获取
    /// 并且针对分库下的特性加接口的支持，然后是分库路由的配置覆盖
    /// 分表下的特性加接口的支持，然后是分表下的路由的配置覆盖
    /// </summary>
    /// <exception cref="ShardingCoreInvalidOperationException"></exception>
    public void Initialize()
    {
        var type = typeof(TEntity);

        //设置标签
        if (_routeOptions.TryGetVirtualDatasourceRoute<TEntity>(out var virtualDatasourceRouteType))
        {
            var metadata = DatasourceShardingMetadata.Create(type);
            var metadataBuilder = DatasourceMetadataBuilder<TEntity>.Create(metadata);
            //配置属性分库信息
            var datasourceRoute = CreateVirtualDataSourceRoute(virtualDatasourceRouteType);
            if (datasourceRoute is Core.Metadata.IMetadataInitializer entityMetadataAutoBindInitializer)
            {
                entityMetadataAutoBindInitializer.Initialize(metadata);
            }

            //配置分库信息
            if (datasourceRoute is IMetadataConfiguration<TEntity> entityMetadataDataSourceConfiguration)
            {
                entityMetadataDataSourceConfiguration.Configure(metadataBuilder);
            }

            _dataSourceRouteManager.AddDatasourceRoute(datasourceRoute);

            if (!_metadataManager.AddMetadata(metadata))
                throw new ShardingCoreInvalidOperationException($"repeat add entity metadata {type.FullName}");
        }

        if (_routeOptions.TryGetVirtualTableRoute<TEntity>(out var virtualTableRouteType))
        {
            var metadata = TableShardingMetadata.Create(type);
            var entityMetadataTableBuilder = TableMetadataBuilder<TEntity>.Create(metadata);
            //配置属性分表信息

            var virtualTableRoute = CreateVirtualTableRoute(virtualTableRouteType);
            if (virtualTableRoute is Core.Metadata.IMetadataInitializer entityMetadataAutoBindInitializer)
            {
                entityMetadataAutoBindInitializer.Initialize(metadata);
            }

            //配置分表信息
            if (virtualTableRoute is IMetadataConfiguration<TEntity> createEntityMetadataTableConfiguration)
            {
                createEntityMetadataTableConfiguration.Configure(entityMetadataTableBuilder);
            }

            //创建虚拟表
            _tableRouteManager.AddRoute(virtualTableRoute);

            if (!_metadataManager.AddMetadata(metadata))
                throw new ShardingCoreInvalidOperationException($"repeat add entity metadata {type.FullName}");
        }
    }

    private IVirtualDatasourceRoute<TEntity> CreateVirtualDataSourceRoute(Type virtualRouteType)
    {
        var instance = _shardingProvider.CreateInstance(virtualRouteType);
        return (IVirtualDatasourceRoute<TEntity>) instance;
    }


    private IVirtualTableRoute<TEntity> CreateVirtualTableRoute(Type virtualRouteType)
    {
        var instance = _shardingProvider.CreateInstance(virtualRouteType);
        return (IVirtualTableRoute<TEntity>) instance;
    }
}
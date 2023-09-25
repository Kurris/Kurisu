using Kurisu.EFSharding.Bootstrap;
using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.DbContextOptionBuilderCreator;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.ModelCacheLockerProviders;
using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;
using Kurisu.EFSharding.Core.QueryTrackers;
using Kurisu.EFSharding.Core.ShardingConfigurations;
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Core.ShardingMigrations.Abstractions;
using Kurisu.EFSharding.Core.ShardingPage.Abstractions;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Dynamicdatasources;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingComparision.Abstractions;
using Kurisu.EFSharding.TableCreator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding.Core.RuntimeContexts;

public sealed class ShardingRuntimeContext<TDbContext> : IShardingRuntimeContext<TDbContext>
    where TDbContext : IShardingDbContext
{
    private readonly IServiceProvider _shardingInternalProvider;

    public Type DbContextType => typeof(TDbContext);

    private ShardingRuntimeContext(Action<IServiceCollection> configure)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        configure(serviceCollection);
        _shardingInternalProvider = serviceCollection.BuildServiceProvider(true);
    }

    public static ShardingRuntimeContext<TDbContext> Create(Action<IServiceCollection> configure)
    {
        var instance = new ShardingRuntimeContext<TDbContext>(configure);
        instance.GetRequiredService<IShardingInitializer>().Initialize();
        return instance;
    }


    private IModelCacheLockerProvider _modelCacheLockerProvider;

    public IModelCacheLockerProvider GetModelCacheLockerProvider()
    {
        return _modelCacheLockerProvider ??= GetRequiredService<IModelCacheLockerProvider>();
    }


    private IShardingProvider _shardingProvider;


    public IShardingProvider GetShardingProvider()
    {
        return _shardingProvider ??= GetRequiredService<IShardingProvider>();
    }

    private IDbContextOptionBuilderCreator _dbContextOptionBuilderCreator;

    public IDbContextOptionBuilderCreator GetDbContextOptionBuilderCreator()
    {
        return _dbContextOptionBuilderCreator ??= GetRequiredService<IDbContextOptionBuilderCreator>();
    }

    private ShardingOptions _shardingConfigOptions;

    public ShardingOptions GetShardingConfigOptions()
    {
        return _shardingConfigOptions ??= GetRequiredService<ShardingOptions>();
    }


    private IRouteOptions _shardingRouteConfigOptions;

    public IRouteOptions GetShardingRouteConfigOptions()
    {
        return _shardingRouteConfigOptions ??= GetRequiredService<IRouteOptions>();
    }

    private IShardingMigrationManager _shardingMigrationManager;

    public IShardingMigrationManager GetShardingMigrationManager()
    {
        return _shardingMigrationManager ??= GetRequiredService<IShardingMigrationManager>();
    }


    private IShardingComparer _shardingComparer;

    public IShardingComparer GetShardingComparer()
    {
        return _shardingComparer ??= GetRequiredService<IShardingComparer>();
    }

    private IShardingCompilerExecutor _shardingCompilerExecutor;

    public IShardingCompilerExecutor GetShardingCompilerExecutor()
    {
        return _shardingCompilerExecutor ??= GetRequiredService<IShardingCompilerExecutor>();
    }

    private IShardingReadWriteManager _shardingReadWriteManager;

    public IShardingReadWriteManager GetShardingReadWriteManager()
    {
        return _shardingReadWriteManager ??= GetRequiredService<IShardingReadWriteManager>();
    }


    private ITrackerManager _trackerManager;

    public ITrackerManager GetTrackerManager()
    {
        return _trackerManager ??= GetRequiredService<ITrackerManager>();
    }


    private IDbContextCreator _dbContextCreator;

    public IDbContextCreator GetDbContextCreator()
    {
        return _dbContextCreator ??= GetRequiredService<IDbContextCreator>();
    }

    private IMetadataManager _entityMetadataManager;

    public IMetadataManager GetMetadataManager()
    {
        return _entityMetadataManager ??= GetRequiredService<IMetadataManager>();
    }

    private IVirtualDatasource _virtualDatasource;

    public IVirtualDatasource GetVirtualDatasource()
    {
        return _virtualDatasource ??= GetRequiredService<IVirtualDatasource>();
    }

    private IDatasourceRouteManager _datasourceRouteManager;

    public IDatasourceRouteManager GetDatasourceRouteManager()
    {
        return _datasourceRouteManager ??= GetRequiredService<IDatasourceRouteManager>();
    }

    private ITableRouteManager _tableRouteManager;

    public ITableRouteManager GetTableRouteManager()
    {
        return _tableRouteManager ??= GetRequiredService<ITableRouteManager>();
    }

    private IReadWriteConnectorFactory _readWriteConnectorFactory;

    public IReadWriteConnectorFactory GetReadWriteConnectorFactory()
    {
        return _readWriteConnectorFactory ??= GetRequiredService<IReadWriteConnectorFactory>();
    }

    private IShardingTableCreator _shardingTableCreator;

    public IShardingTableCreator GetShardingTableCreator()
    {
        return _shardingTableCreator ??= GetRequiredService<IShardingTableCreator>();
    }

    private IRouteTailFactory _routeTailFactory;

    public IRouteTailFactory GetRouteTailFactory()
    {
        return _routeTailFactory ??= GetRequiredService<IRouteTailFactory>();
    }

    private IQueryTracker _queryTracker;

    public IQueryTracker GetQueryTracker()
    {
        return _queryTracker ??= GetRequiredService<IQueryTracker>();
    }

    private IUnionAllMergeManager _unionAllMergeManager;

    public IUnionAllMergeManager GetUnionAllMergeManager()
    {
        return _unionAllMergeManager ??= GetRequiredService<IUnionAllMergeManager>();
    }

    private IShardingPageManager _shardingPageManager;

    public IShardingPageManager GetShardingPageManager()
    {
        return _shardingPageManager ??= GetRequiredService<IShardingPageManager>();
    }

    private IDatasourceInitializer _datasourceInitializer;

    public IDatasourceInitializer GetDatasourceInitializer()
    {
        return _datasourceInitializer ??= GetRequiredService<IDatasourceInitializer>();
    }

    private IParallelTableManager _parallelTableManager;

    public IParallelTableManager GetParallelTableManager()
    {
        return _parallelTableManager ??= GetRequiredService<IParallelTableManager>();
    }


    public void GetOrCreateShardingRuntimeModel(DbContext context)
    {
        var metadataManager = GetService<IMetadataManager>();
        var trackerManager = GetService<ITrackerManager>();

        var entityTypes = context.Model.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            trackerManager.AddDbContextModel(entityType.ClrType, entityType.FindPrimaryKey() != null);
            var isOwned = entityType.IsOwned();
            if (!isOwned)
            {
                if (!metadataManager.IsSharding(entityType.ClrType))
                {
                    // var entityMetadata = new ShardingMetadata(entityType.ClrType);
                    // metadataManager.AddEntityMetadata(entityMetadata);
                }

                metadataManager.InitModel(entityType);
            }
        }
    }

    public object GetService(Type serviceType)
    {
        return _shardingInternalProvider.GetService(serviceType);
    }

    public TService GetService<TService>()
    {
        return _shardingInternalProvider.GetService<TService>();
    }

    public object GetRequiredService(Type serviceType)
    {
        return _shardingInternalProvider.GetRequiredService(serviceType);
    }

    public TService GetRequiredService<TService>()
    {
        return _shardingInternalProvider.GetRequiredService<TService>();
    }

    public IShardingRouteManager GetShardingRouteManager()
    {
        return GetRequiredService<IShardingRouteManager>();
    }
}
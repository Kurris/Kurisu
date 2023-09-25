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

namespace Kurisu.EFSharding.Core.RuntimeContexts;

public interface IShardingRuntimeContext
{
    Type DbContextType { get; }
    IModelCacheLockerProvider GetModelCacheLockerProvider();
    IShardingProvider GetShardingProvider();
    IDbContextOptionBuilderCreator GetDbContextOptionBuilderCreator();
    ShardingOptions GetShardingConfigOptions();
    IRouteOptions GetShardingRouteConfigOptions();
    IShardingMigrationManager GetShardingMigrationManager();
    IShardingComparer GetShardingComparer();
    IShardingCompilerExecutor GetShardingCompilerExecutor();
    IShardingReadWriteManager GetShardingReadWriteManager();
    IShardingRouteManager GetShardingRouteManager();
    ITrackerManager GetTrackerManager();
    IDbContextCreator GetDbContextCreator();

    IMetadataManager GetMetadataManager();

    IVirtualDatasource GetVirtualDatasource();
    IDatasourceRouteManager GetDatasourceRouteManager();
    ITableRouteManager GetTableRouteManager();
    IShardingTableCreator GetShardingTableCreator();
    IRouteTailFactory GetRouteTailFactory();
    IReadWriteConnectorFactory GetReadWriteConnectorFactory();
    IQueryTracker GetQueryTracker();
    IUnionAllMergeManager GetUnionAllMergeManager();
    IShardingPageManager GetShardingPageManager();
    IDatasourceInitializer GetDatasourceInitializer();

    IParallelTableManager GetParallelTableManager();

    void GetOrCreateShardingRuntimeModel(DbContext dbContext);
    object GetService(Type serviceType);
    TService GetService<TService>();
    object GetRequiredService(Type serviceType);
    TService GetRequiredService<TService>();
}
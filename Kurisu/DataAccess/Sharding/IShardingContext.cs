using System;
using Kurisu.DataAccess.Sharding.Configurations;
using Kurisu.DataAccess.Sharding.DependencyInjection;
using Kurisu.DataAccess.Sharding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Sharding;

public interface IShardingContext
{
    Type DbContextType { get; }

    IModelCacheLockerProvider GetModelCacheLockerProvider();

    // IDbContextTypeAware GetDbContextTypeAware();
    IShardingProvider GetShardingProvider();

    // IDbContextOptionBuilderCreator GetDbContextOptionBuilderCreator();
    ShardingOptions Options { get; }

    // IShardingRouteConfigOptions GetShardingRouteConfigOptions();
    // IShardingMigrationManager GetShardingMigrationManager();
    // IShardingComparer GetShardingComparer();
    // IShardingCompilerExecutor GetShardingCompilerExecutor();
    // IShardingReadWriteManager GetShardingReadWriteManager();
    // IShardingRouteManager GetShardingRouteManager();
    // ITrackerManager GetTrackerManager();
    // IParallelTableManager GetParallelTableManager();
    // IDbContextCreator GetDbContextCreator();
    IEntityMetadataManager GetEntityMetadataManager();


    // IVirtualDataSource GetVirtualDataSource();
    // IDataSourceRouteManager GetDataSourceRouteManager();
    // ITableRouteManager GetTableRouteManager();
    // IShardingTableCreator GetShardingTableCreator();
    // IRouteTailFactory GetRouteTailFactory();
    // IReadWriteConnectorFactory GetReadWriteConnectorFactory();
    // IQueryTracker GetQueryTracker();
    // IUnionAllMergeManager GetUnionAllMergeManager();
    // IShardingPageManager GetShardingPageManager();
    // IDataSourceInitializer GetDataSourceInitializer();

    void GetOrCreateShardingRuntimeModel(DbContext dbContext);
    object GetService(Type serviceType);
    T GetService<T>();
    object GetRequiredService(Type serviceType);
    T GetRequiredService<T>();
}

public interface IShardingContext<TDbContext> : IShardingContext where TDbContext : DbContext, IShardingDbContext
{
}
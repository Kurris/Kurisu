using Kurisu.EFSharding.Bootstrap;
using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.DbContextOptionBuilderCreator;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.ModelCacheLockerProviders;
using Kurisu.EFSharding.Core.QueryRouteManagers;
using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;
using Kurisu.EFSharding.Core.QueryTrackers;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.ShardingConfigurations;
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Core.ShardingMigrations;
using Kurisu.EFSharding.Core.ShardingMigrations.Abstractions;
using Kurisu.EFSharding.Core.ShardingPage;
using Kurisu.EFSharding.Core.ShardingPage.Abstractions;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Core.UnionAllMergeShardingProviders;
using Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.datasourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Dynamicdatasources;
using Kurisu.EFSharding.DynamicDataSources;
using Kurisu.EFSharding.Sharding;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.MergeContexts;
using Kurisu.EFSharding.Sharding.Parsers;
using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingComparision;
using Kurisu.EFSharding.Sharding.ShardingComparision.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingExecutors;
using Kurisu.EFSharding.Sharding.ShardingExecutors.NativeTrackQueries;
using Kurisu.EFSharding.TableCreator;
using Kurisu.EFSharding.TableExists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.EFSharding;

/// <summary>
/// 分片运行时建造者
/// </summary>
/// <typeparam name="TShardingDbContext"></typeparam>
internal class ShardingRuntimeBuilder<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();

    private Action<IShardingProvider, IRouteOptions> _shardingRouteConfigOptionsConfigure;

    public void UseRouteConfig(Action<IRouteOptions> configure)
    {
        UseRouteConfig((_, options) => { configure.Invoke(options); });
    }

    public ShardingRuntimeBuilder<TShardingDbContext> UseRouteConfig(Action<IShardingProvider, IRouteOptions> configure)
    {
        _shardingRouteConfigOptionsConfigure = configure;
        return this;
    }

    private Action<IShardingProvider, ShardingOptions> _shardingConfigOptionsConfigure;

    public ShardingRuntimeBuilder<TShardingDbContext> UseConfig(Action<ShardingOptions> configure)
    {
        return UseConfig((sp, options) => { configure.Invoke(options); });
    }

    public ShardingRuntimeBuilder<TShardingDbContext> UseConfig(Action<IShardingProvider, ShardingOptions> configure)
    {
        _shardingConfigOptionsConfigure = configure;
        return this;
    }

    public ShardingRuntimeBuilder<TShardingDbContext> ReplaceService<TService, TImplement>(ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplement), lifetime);
        _serviceActions.Add(s => s.Replace(descriptor));
        return this;
    }

    public IShardingRuntimeContext<TShardingDbContext> Build(IServiceProvider appServiceProvider)
    {
        return ShardingRuntimeContext<TShardingDbContext>.Create(services =>
        {
            services.AddSingleton<IShardingProvider>(sp => new ShardingProvider(sp, appServiceProvider));

            services.AddSingleton<IRouteOptions>(sp =>
            {
                var shardingProvider = sp.GetRequiredService<IShardingProvider>();
                var shardingRouteConfigOptions = new ShardingRouteConfigOptions();
                _shardingRouteConfigOptionsConfigure?.Invoke(shardingProvider, shardingRouteConfigOptions);
                return shardingRouteConfigOptions;
            });

            services.AddSingleton(sp =>
            {
                var shardingProvider = sp.GetRequiredService<IShardingProvider>();
                var shardingConfigOptions = new ShardingOptions();
                _shardingConfigOptionsConfigure?.Invoke(shardingProvider, shardingConfigOptions);
                return shardingConfigOptions;
            });
            services.AddLogging();

            services.TryAddSingleton<IShardingInitializer, ShardingInitializer>();
            services.TryAddSingleton<IModelCacheLockerProvider, DefaultModelCacheLockerProvider>();
            services.TryAddSingleton<IDatasourceInitializer, DatasourceInitializer>();
            services.TryAddSingleton<ITableRouteManager, TableRouteManager>();
            services.TryAddSingleton<IVirtualDatasourceConfigurationParams, SimpleVirtualDataSourceConfigurationParams>();

            //分表dbcontext创建
            services.TryAddSingleton<IDbContextCreator, DbContextCreator<TShardingDbContext>>();
            services.TryAddSingleton<IDbContextOptionBuilderCreator, ActivatorDbContextOptionBuilderCreator>();


            // services.TryAddSingleton<IDataSourceInitializer<TShardingDbContext>, DataSourceInitializer<TShardingDbContext>>();
            services.TryAddSingleton<ITrackerManager, TrackerManager>();
            services.TryAddSingleton<IStreamMergeContextFactory, StreamMergeContextFactory>();
            services.TryAddSingleton<IShardingTableCreator, ShardingTableCreator>();
            //虚拟数据源管理
            services.TryAddSingleton<IVirtualDatasource, VirtualDatasource>();
            services.TryAddSingleton<IDatasourceRouteManager, DatasourceRouteManager>();
            services.TryAddSingleton<IDatasourceRouteRuleEngine, DatasourceRouteRuleEngine>();
            services.TryAddSingleton<IDatasourceRouteRuleEngineFactory, DatasourceRouteRuleEngineFactory>();
            //读写分离链接创建工厂
            services.TryAddSingleton<IShardingReadWriteAccessor, ShardingReadWriteAccessor>();
            services.TryAddSingleton<IReadWriteConnectorFactory, ReadWriteConnectorFactory>();

            //虚拟表管理
            //分表分库对象元信息管理
            services.TryAddSingleton<IMetadataManager, MetadataManager>();

            //分表引擎
            services.TryAddSingleton<ITableRouteRuleEngineFactory, TableRouteRuleEngineFactory>();
            services.TryAddSingleton<ITableRouteRuleEngine, TableRouteRuleEngine>();
            //分表引擎工程
            services.TryAddSingleton<IParallelTableManager, ParallelTableManager>();
            services.TryAddSingleton<IRouteTailFactory, RouteTailFactory>();
            services.TryAddSingleton<IShardingCompilerExecutor, DefaultShardingCompilerExecutor>();
            services.TryAddSingleton<IQueryCompilerContextFactory, QueryCompilerContextFactory>();
            services.TryAddSingleton<IShardingQueryExecutor, DefaultShardingQueryExecutor>();

            //
            services.TryAddSingleton<IPrepareParser, DefaultPrepareParser>();
            services.TryAddSingleton<IQueryableParseEngine, QueryableParseEngine>();
            services.TryAddSingleton<IQueryableRewriteEngine, QueryableRewriteEngine>();
            services.TryAddSingleton<IQueryableOptimizeEngine, QueryableOptimizeEngine>();

            //migration manage
            services.TryAddSingleton<IShardingMigrationAccessor, ShardingMigrationAccessor>();
            services.TryAddSingleton<IShardingMigrationManager, ShardingMigrationManager>();

            //route manage
            services.TryAddSingleton<IShardingRouteManager, ShardingRouteManager>();
            services.TryAddSingleton<IShardingRouteAccessor, ShardingRouteAccessor>();

            //sharding page
            services.TryAddSingleton<IShardingPageManager, ShardingPageManager>();
            services.TryAddSingleton<IShardingPageAccessor, ShardingPageAccessor>();

            services.TryAddSingleton<IUnionAllMergeManager, UnionAllMergeManager>();
            services.TryAddSingleton<IUnionAllMergeAccessor, UnionAllMergeAccessor>();
            services.TryAddSingleton<IQueryTracker, QueryTracker>();
            services.TryAddSingleton<IShardingTrackQueryExecutor, DefaultShardingTrackQueryExecutor>();
            services.TryAddSingleton<INativeTrackQueryExecutor, NativeTrackQueryExecutor>();
            //读写分离手动指定
            services.TryAddSingleton<IShardingReadWriteManager, ShardingReadWriteManager>();
            services.TryAddSingleton<IShardingComparer, CSharpLanguageShardingComparer>();

            services.TryAddSingleton<ITableEnsureManager, MySqlTableEnsureManager>();


            foreach (var serviceAction in _serviceActions)
            {
                serviceAction.Invoke(services);
            }
        });
    }
}
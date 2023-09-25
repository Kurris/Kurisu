using System.Diagnostics.CodeAnalysis;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.ShardingConfigurations.ConfigBuilders;
using Kurisu.EFSharding.EFCores;
using Kurisu.EFSharding.EFCores.EFCore6x;
using Kurisu.EFSharding.EFCores.EFCore6x.Tx;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public static class ShardingCoreExtension
{
    /// <summary>
    /// 添加ShardingCore配置和EntityFrameworkCore的<![CDATA[services.AddDbContext<TShardingDbContext>]]>
    /// </summary>
    /// <typeparam name="TShardingDbContext"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static ShardingCoreConfigBuilder<TShardingDbContext> AddShardingDbContext<TShardingDbContext>(
        this IServiceCollection services)
        where TShardingDbContext : DbContext, IShardingDbContext
    {
        services.AddDbContext<TShardingDbContext>((provider, builder) =>
        {
            var shardingRuntimeContext = provider.GetRequiredService<IShardingRuntimeContext<TShardingDbContext>>();

            var shardingConfigOptions = shardingRuntimeContext.GetShardingConfigOptions();
            shardingConfigOptions.ShardingMigrationConfigure?.Invoke(builder);
            var virtualDataSource = shardingRuntimeContext.GetVirtualDatasource();
            var connectionString = virtualDataSource.GetConnectionString(virtualDataSource.DefaultDatasourceName);

            virtualDataSource.ConfigurationParams
                .UseDbContextOptionsBuilder(connectionString, builder)
                .UseShardingWrapMark()
                .UseShardingMigrator()
                .UseShardingOptions(shardingRuntimeContext)
                .ReplaceService<IQueryCompiler, ShardingQueryCompiler>()
                .ReplaceService<IDbSetInitializer, ShardingDbSetInitializer>()
                .ReplaceService<IChangeTrackerFactory, ShardingChangeTrackerFactory>()
                .ReplaceService<IDbContextTransactionManager, ShardingRelationalTransactionManager>()
                .ReplaceService<IStateManager, ShardingStateManager>()
                .ReplaceService<IRelationalTransactionFactory, ShardingRelationalTransactionFactory>();

            virtualDataSource.ConfigurationParams.UseShellDbContextOptionBuilder(builder);
        });

        return new ShardingCoreConfigBuilder<TShardingDbContext>(services);
    }

    public static DbContextOptionsBuilder UseShardingMigrator(
        this DbContextOptionsBuilder optionsBuilder)
    {
        return optionsBuilder.ReplaceService<IMigrator, ShardingMigrator>();
    }

    public static DbContextOptionsBuilder UseShardingOptions(this DbContextOptionsBuilder optionsBuilder,
        IShardingRuntimeContext shardingRuntimeContext)
    {
        var shardingOptionsExtension = optionsBuilder.CreateOrGetShardingOptionsExtension(shardingRuntimeContext);
        ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(shardingOptionsExtension);
        return optionsBuilder;
    }


    private static DbContextOptionsBuilder UseShardingWrapMark(this DbContextOptionsBuilder optionsBuilder)
    {
        var shardingWrapExtension = optionsBuilder.CreateOrGetShardingWrapExtension();
        ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(shardingWrapExtension);
        return optionsBuilder;
    }

    private static ShardingWrapOptionsExtension CreateOrGetShardingWrapExtension(
        this DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.Options.FindExtension<ShardingWrapOptionsExtension>() ??
           new ShardingWrapOptionsExtension();

    private static ShardingOptionsExtension CreateOrGetShardingOptionsExtension(
        this DbContextOptionsBuilder optionsBuilder, IShardingRuntimeContext shardingRuntimeContext) =>
        optionsBuilder.Options.FindExtension<ShardingOptionsExtension>() ??
        new ShardingOptionsExtension(shardingRuntimeContext);


    public static void UseInnerDbContextSharding(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ShardingModelCacheKeyFactory>()
            .ReplaceService<IModelSource, ShardingModelSource>()
            .ReplaceService<IModelCustomizer, ShardingModelCustomizer>();
    }

    public static async Task UseAutoTryCompensateTable(this IServiceProvider serviceProvider, int parallelCount = 4)
    {
        var shardingRuntimeContext = serviceProvider.GetRequiredService<IShardingRuntimeContext>();
        await shardingRuntimeContext.UseAutoTryCompensateTable(parallelCount);
    }
}
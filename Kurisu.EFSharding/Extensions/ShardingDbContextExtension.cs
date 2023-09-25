using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.EFCores.EFCore6x;
using Kurisu.EFSharding.Sharding;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingDbContextExtension
{
    public static bool IsShellDbContext(this DbContext dbContext)
    {
        return dbContext.GetService<IDbContextOptions>().FindExtension<ShardingWrapOptionsExtension>() != null;
    }

    public static IShardingDbContextExecutor CreateShardingDbContextExecutor<TDbContext>(
        this TDbContext shellDbContext)
        where TDbContext : DbContext, IShardingDbContext
    {
        return shellDbContext.IsShellDbContext() ? new ShardingDbContextExecutor(shellDbContext) : default(IShardingDbContextExecutor);
    }

    public static bool IsUseReadWriteSeparation(this IShardingDbContext shardingDbContext)
    {
        return shardingDbContext.GetShardingExecutor().GetVirtualDatasource().UseReadWriteSeparation;
    }

    public static bool SupportUnionAllMerge(this IShardingDbContext shardingDbContext)
    {
        var dbContext = (DbContext) shardingDbContext;
        return dbContext.GetService<IDbContextServices>().ContextOptions.FindExtension<UnionAllMergeOptionsExtension>() is not null;
    }

    /// <summary>
    /// 创建共享链接DbConnection
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <param name="dataSourceName"></param>
    /// <param name="routeTail"></param>
    /// <returns></returns>
    public static DbContext GetShareDbContext(this IShardingDbContext shardingDbContext, string dataSourceName, IRouteTail routeTail)
    {
        return shardingDbContext.GetShardingExecutor().CreateDbContext(CreateDbContextStrategyEnum.ShareConnection, dataSourceName, routeTail);
    }

    /// <summary>
    /// 获取独立生命周期的写连接字符串的db context
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <param name="dataSourceName"></param>
    /// <param name="routeTail"></param>
    /// <returns></returns>
    public static DbContext GetIndependentWriteDbContext(this IShardingDbContext shardingDbContext, string dataSourceName, IRouteTail routeTail)
    {
        return shardingDbContext.GetShardingExecutor().CreateDbContext(CreateDbContextStrategyEnum.IndependentConnectionWrite, dataSourceName, routeTail);
    }

    /// <summary>
    /// 获取独立生命周期的读连接字符串的db context
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <param name="dataSourceName"></param>
    /// <param name="routeTail"></param>
    /// <returns></returns>
    public static DbContext GetIndependentQueryDbContext(this IShardingDbContext shardingDbContext, string dataSourceName, IRouteTail routeTail)
    {
        return shardingDbContext.GetShardingExecutor().CreateDbContext(CreateDbContextStrategyEnum.IndependentConnectionQuery, dataSourceName, routeTail);
    }
}
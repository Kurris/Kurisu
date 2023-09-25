using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.DbContextCreator;

internal class DbContextCreator<TShardingDbContext> : IDbContextCreator where TShardingDbContext : DbContext, IShardingDbContext
{
    private readonly Func<ShardingDbContextOptions, DbContext> _creator;

    public DbContextCreator()
    {
        ShardingCoreHelper.CheckContextConstructors<TShardingDbContext>();
        _creator = ShardingCoreHelper.CreateActivator<TShardingDbContext>();
    }

    /// <summary>
    /// 创建DbContext
    /// </summary>
    /// <param name="shellDbContext"></param>
    /// <param name="shardingDbContextOptions"></param>
    /// <returns></returns>
    public virtual DbContext CreateDbContext(DbContext shellDbContext, ShardingDbContextOptions shardingDbContextOptions)
    {
        var dbContext = _creator(shardingDbContextOptions);

        if (dbContext is IShardingDbContext shardingTableDbContext)
        {
            shardingTableDbContext.RouteTail = shardingDbContextOptions.RouteTail;
        }

        //触发model构建,
        _ = dbContext.Model;
        return dbContext;
    }

    public virtual DbContext GetShellDbContext(IShardingProvider shardingProvider)
    {
        return shardingProvider.GetService<TShardingDbContext>();
    }
}
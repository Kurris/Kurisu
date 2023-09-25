using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.DbContextOptionBuilderCreator;

public class ActivatorDbContextOptionBuilderCreator : IDbContextOptionBuilderCreator
{
    private readonly IShardingProvider _shardingProvider;

    public ActivatorDbContextOptionBuilderCreator(IShardingProvider shardingProvider)
    {
        _shardingProvider = shardingProvider;
    }

    public DbContextOptionsBuilder CreateDbContextOptionBuilder()
    {
        var dbContextType = _shardingProvider.GetRequiredService<IShardingRuntimeContext>().DbContextType;
        var type = typeof(DbContextOptionsBuilder<>);
        type = type.MakeGenericType(dbContextType);
        var dbContextOptionsBuilder = (DbContextOptionsBuilder) Activator.CreateInstance(type);
        if (_shardingProvider.ApplicationServiceProvider != null)
        {
            dbContextOptionsBuilder.UseApplicationServiceProvider(_shardingProvider.ApplicationServiceProvider);
        }

        return dbContextOptionsBuilder;
    }
}
// ReSharper disable CheckNamespace

using Kurisu.DataAccess.Sharding;
using Kurisu.DataAccess.Sharding.Builder;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ShardingServiceCollectionExtensions
{
    public static IShardingServiceBuilder AddSharding<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IShardingDbContext
    {
        services.AddDbContext<TDbContext>((provider, builder) =>
        {
            var shardingRuntimeContext = provider.GetRequiredService<IShardingContext<TDbContext>>();


        });
        return new ShardingServiceBuilder<TDbContext>();
    }
}
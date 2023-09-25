using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Sharding.Builder;

public class ShardingServiceBuilder<TShardingDbContext> : IShardingServiceBuilder
    where TShardingDbContext : DbContext, IShardingDbContext
{
    private readonly IShardingInternalBuilder _shardingInternalBuilder;

    public ShardingServiceBuilder()
    {
        _shardingInternalBuilder = new ShardingInternalBuilder<TShardingDbContext>();
    }
}
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Sharding.Builder;

internal class ShardingInternalBuilder<TDbContext> : IShardingInternalBuilder where TDbContext : DbContext, IShardingDbContext
{
}
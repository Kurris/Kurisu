using Kurisu.EFSharding.Sharding;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.Test.WebApi_A;

public class DefaultShardingDbContext : BaseShardingDbContext
{
    public DbSet<Entity.Test> Tests { get; set; }

    public DefaultShardingDbContext(DbContextOptions options) : base(options)
    {
    }
}
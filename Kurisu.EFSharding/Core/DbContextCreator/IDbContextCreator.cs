using Kurisu.EFSharding.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.DbContextCreator;

public interface IDbContextCreator
{
    DbContext CreateDbContext(DbContext shellDbContext, ShardingDbContextOptions shardingDbContextOptions);

    DbContext GetShellDbContext(IShardingProvider shardingProvider);
}
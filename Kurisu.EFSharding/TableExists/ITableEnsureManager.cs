using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.TableExists;

public interface ITableEnsureManager
{
    Task<ISet<string>> GetExistTablesAsync(IShardingDbContext shardingDbContext, string datasourceName);
}
using Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface ICurrentDbContextDiscover
{
    IDictionary<string, IDatasourceDbContext> GetCurrentDbContexts();
}
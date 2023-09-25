using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.EFCores;

public interface IShardingDbContextAvailable
{
    IShardingDbContext GetShardingDbContext();
}
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Core.QueryTrackers;

public interface IQueryTracker
{
    public object? Track(object entity, IShardingDbContext shardingDbContext);
}
using System.Collections;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerators;

namespace Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerables;

public class TrackEnumerable<T>:IEnumerable<T>
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IEnumerable<T> _enumerable;

    public TrackEnumerable(IShardingDbContext shardingDbContext,IEnumerable<T> enumerable)
    {
        _shardingDbContext = shardingDbContext;
        _enumerable = enumerable;
    }
    public IEnumerator<T> GetEnumerator()
    {
        return new TrackerEnumerator<T>(_shardingDbContext,_enumerable.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
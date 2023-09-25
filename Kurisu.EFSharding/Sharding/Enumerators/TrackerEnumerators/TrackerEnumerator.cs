using System.Collections;
using Kurisu.EFSharding.Core.QueryTrackers;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerators;

internal class TrackerEnumerator<T>: IEnumerator<T>
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IEnumerator<T> _enumerator;
    private readonly IQueryTracker _queryTrack;

    public TrackerEnumerator(IShardingDbContext shardingDbContext,IEnumerator<T> enumerator)
    {
        var shardingRuntimeContext = ((DbContext)shardingDbContext).GetShardingRuntimeContext();
        _shardingDbContext = shardingDbContext;
        _enumerator = enumerator;
        _queryTrack = shardingRuntimeContext.GetQueryTracker();
    }
    public bool MoveNext()
    {
        return _enumerator.MoveNext();
    }

    public void Reset()
    {
        _enumerator.Reset();
    }

    public T Current => GetCurrent();

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        _enumerator.Dispose();
    }
    private T GetCurrent()
    {
        var current = _enumerator.Current;
        if (current != null)
        {
            var attachedEntity = _queryTrack.Track(current, _shardingDbContext);
            if (attachedEntity != null)
            {
                return (T)attachedEntity;
            }
        }
        return current;
    }
}
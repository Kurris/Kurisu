using Kurisu.EFSharding.Core.QueryTrackers;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerators;

#if !EFCORE2

internal class AsyncTrackerEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IAsyncEnumerator<T> _asyncEnumerator;
    private readonly IQueryTracker _queryTrack;

    public AsyncTrackerEnumerator(IShardingDbContext shardingDbContext, IAsyncEnumerator<T> asyncEnumerator)
    {
        var shardingRuntimeContext = ((DbContext)shardingDbContext).GetShardingRuntimeContext();
        _shardingDbContext = shardingDbContext;
        _asyncEnumerator = asyncEnumerator;
        _queryTrack = shardingRuntimeContext.GetQueryTracker();
    }
    public ValueTask DisposeAsync()
    {
        return _asyncEnumerator.DisposeAsync();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return _asyncEnumerator.MoveNextAsync();
    }

    public T Current => GetCurrent();
    private T GetCurrent()
    {
        var current = _asyncEnumerator.Current;
        if (current != null)
        {
            var attachedEntity = _queryTrack.Track(current, _shardingDbContext);
            if (attachedEntity!=null)
            {
                return (T)attachedEntity;
            }
        }

        return current;
    }
}
#endif

#if EFCORE2

    public class AsyncTrackerEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IShardingDbContext _shardingDbContext;
        private readonly IAsyncEnumerator<T> _asyncEnumerator;
        private readonly IQueryTracker _queryTrack;

        public AsyncTrackerEnumerator(IShardingDbContext shardingDbContext, IAsyncEnumerator<T> asyncEnumerator)
        {
            var shardingRuntimeContext = ((DbContext)shardingDbContext).GetShardingRuntimeContext();
            _shardingDbContext = shardingDbContext;
            _asyncEnumerator = asyncEnumerator;
            _queryTrack = shardingRuntimeContext.GetQueryTracker();
        }

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return _asyncEnumerator.MoveNext(cancellationToken);
        }

        public T Current => GetCurrent();
        private T GetCurrent()
        {
            var current = _asyncEnumerator.Current;
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

        public void Dispose()
        {
            _asyncEnumerator.Dispose();
        }
    }
#endif
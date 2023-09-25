using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerators;

namespace Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerables;

public class AsyncTrackerEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IAsyncEnumerable<T> _asyncEnumerable;

    public AsyncTrackerEnumerable(IShardingDbContext shardingDbContext, IAsyncEnumerable<T> asyncEnumerable)
    {
        _shardingDbContext = shardingDbContext;
        _asyncEnumerable = asyncEnumerable;
    }
#if !EFCORE2
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        return new AsyncTrackerEnumerator<T>(_shardingDbContext,_asyncEnumerable.GetAsyncEnumerator(cancellationToken));
    }
#endif

#if EFCORE2
        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new AsyncTrackerEnumerator<T>(_shardingDbContext, _asyncEnumerable.GetEnumerator());
        }
#endif
}
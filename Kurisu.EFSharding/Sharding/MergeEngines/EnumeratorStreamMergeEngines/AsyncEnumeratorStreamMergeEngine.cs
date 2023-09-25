using System.Collections;
using Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerators;
using Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

namespace Kurisu.EFSharding.Sharding.MergeEngines.EnumeratorStreamMergeEngines;

internal class AsyncEnumeratorStreamMergeEngine<T> : IAsyncEnumerable<T>, IEnumerable<T>
{
    private readonly StreamMergeContext _mergeContext;

    public AsyncEnumeratorStreamMergeEngine(StreamMergeContext mergeContext)
    {
        _mergeContext = mergeContext;
    }


    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_mergeContext.TryPrepareExecuteContinueQuery(() => new EmptyQueryEnumerator<T>(),
                out var emptyQueryEnumerator))
        {
            return emptyQueryEnumerator;
        }

        var asyncEnumerator = EnumeratorStreamMergeEngineFactory<T>.Create(_mergeContext).GetStreamEnumerable()
            .GetAsyncEnumerator(cancellationToken);

        return _mergeContext.IsUseShardingTrack(typeof(T))
            ? new AsyncTrackerEnumerator<T>(_mergeContext.GetShardingDbContext(), asyncEnumerator)
            : asyncEnumerator;
    }


    public IEnumerator<T> GetEnumerator()
    {
        if (!_mergeContext.TryPrepareExecuteContinueQuery(() => new EmptyQueryEnumerator<T>(), out var emptyQueryEnumerator))
            return emptyQueryEnumerator;
        var enumerator = ((IEnumerable<T>) EnumeratorStreamMergeEngineFactory<T>.Create(_mergeContext).GetStreamEnumerable())
            .GetEnumerator();

        return _mergeContext.IsUseShardingTrack(typeof(T))
            ? new TrackerEnumerator<T>(_mergeContext.GetShardingDbContext(), enumerator)
            : enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
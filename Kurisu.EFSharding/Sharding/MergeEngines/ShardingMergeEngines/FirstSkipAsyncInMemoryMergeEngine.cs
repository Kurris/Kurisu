using Kurisu.EFSharding.Sharding.MergeEngines.EnumeratorStreamMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class FirstSkipAsyncInMemoryMergeEngine<TEntity> : IEnsureMergeResult<TEntity>
{
    private readonly StreamMergeContext _streamMergeContext;

    public FirstSkipAsyncInMemoryMergeEngine(StreamMergeContext streamMergeContext)
    {
        _streamMergeContext = streamMergeContext;
    }


    public TEntity MergeResult()
    {
        //将toke改成1
        var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);

        var list = asyncEnumeratorStreamMergeEngine.ToStreamList();
        return list.First();
    }

    public async Task<TEntity> MergeResultAsync(CancellationToken cancellationToken = new())
    {
        //将toke改成1
        var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);
        var take = _streamMergeContext.GetTake();
        var list = await asyncEnumeratorStreamMergeEngine.ToStreamListAsync(take, cancellationToken).ConfigureAwait(false);
        return list.First();
    }
}
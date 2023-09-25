using Kurisu.EFSharding.Sharding.MergeEngines.EnumeratorStreamMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;


internal class FirstOrDefaultSkipAsyncInMemoryMergeEngine<TEntity> : IEnsureMergeResult<TEntity>
{
    private readonly StreamMergeContext _streamMergeContext;

    public FirstOrDefaultSkipAsyncInMemoryMergeEngine(StreamMergeContext streamMergeContext)
    {
        _streamMergeContext = streamMergeContext;
    }
    public TEntity MergeResult()
    {
        //将take改成1
        var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);
        var list = asyncEnumeratorStreamMergeEngine.ToStreamList();
        return list.FirstOrDefault();
    }

    public async Task<TEntity> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        //将take改成1
        var asyncEnumeratorStreamMergeEngine = new AsyncEnumeratorStreamMergeEngine<TEntity>(_streamMergeContext);

        var take = _streamMergeContext.GetTake();
        var list = await asyncEnumeratorStreamMergeEngine.ToStreamListAsync(take, cancellationToken).ConfigureAwait(false);
        return list.FirstOrDefault();

    }
}
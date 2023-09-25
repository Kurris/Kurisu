using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class ReverseEnumerableShardingMerger<TEntity> : AbstractEnumerableShardingMerger<TEntity>
{
    public ReverseEnumerableShardingMerger(StreamMergeContext streamMergeContext, bool async) : base(
        streamMergeContext, async)
    {
    }

    public override IStreamMergeAsyncEnumerator<TEntity> StreamMerge(List<IStreamMergeAsyncEnumerator<TEntity>> parallelResults)
    {
        var streamMergeAsyncEnumerator = base.StreamMerge(parallelResults);
        return new InMemoryReverseStreamMergeAsyncEnumerator<TEntity>(streamMergeAsyncEnumerator);
    }
}
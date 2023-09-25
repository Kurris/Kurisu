namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class LastOrDefaultEnumerableShardingMerger<TEntity> : AbstractEnumerableShardingMerger<TEntity>
{
    public LastOrDefaultEnumerableShardingMerger(StreamMergeContext streamMergeContext, bool async) : base(
        streamMergeContext, async)
    {
    }
}
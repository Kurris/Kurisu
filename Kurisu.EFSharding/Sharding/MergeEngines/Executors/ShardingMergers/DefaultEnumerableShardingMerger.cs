namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class DefaultEnumerableShardingMerger<TEntity>:AbstractEnumerableShardingMerger<TEntity>
{
    public DefaultEnumerableShardingMerger(StreamMergeContext streamMergeContext,bool async) : base(streamMergeContext,async)
    {
    }
}
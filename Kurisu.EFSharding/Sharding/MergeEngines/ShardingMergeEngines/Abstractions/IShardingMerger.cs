namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;

internal interface IShardingMerger<TResult>
{
    TResult StreamMerge(List<TResult> parallelResults);
    void InMemoryMerge(List<TResult> beforeInMemoryResults,List<TResult> parallelResults);
}
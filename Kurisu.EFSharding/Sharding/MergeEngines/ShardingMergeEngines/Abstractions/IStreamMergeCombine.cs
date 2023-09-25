using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;

internal interface IStreamMergeCombine
{
    IStreamMergeAsyncEnumerator<TEntity> StreamMergeEnumeratorCombine<TEntity>(StreamMergeContext streamMergeContext,IStreamMergeAsyncEnumerator<TEntity>[] streamsAsyncEnumerators);
}
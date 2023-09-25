using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class DefaultShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    public DefaultShardingEnumerable(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        return new DefaultEnumerableExecutor<TEntity>(GetStreamMergeContext(), async);
    }
}
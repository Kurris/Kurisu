using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class SingleOrDefaultShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    public SingleOrDefaultShardingEnumerable(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        GetStreamMergeContext().ReSetTake(2);
        return new DefaultEnumerableExecutor<TEntity>(GetStreamMergeContext(), async);
    }
}
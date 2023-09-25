using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class FirstOrDefaultShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    public FirstOrDefaultShardingEnumerable(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        GetStreamMergeContext().ReSetTake(1);
        return new DefaultEnumerableExecutor<TEntity>(GetStreamMergeContext(), async);
    }
}
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class LongCountAsyncInMemoryMergeEngine<TEntity> : AbstractMethodEnsureWrapMergeEngine<long>
{
    public LongCountAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }

    protected override IExecutor<RouteQueryResult<long>> CreateExecutor()
    {
        return new LongCountMethodExecutor<TEntity>(GetStreamMergeContext());
    }
}
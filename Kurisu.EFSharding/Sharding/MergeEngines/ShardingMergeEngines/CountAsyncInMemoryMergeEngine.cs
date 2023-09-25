using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class CountAsyncInMemoryMergeEngine<TEntity> : AbstractMethodEnsureWrapMergeEngine<int>
{
    public CountAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }
    protected override IExecutor<RouteQueryResult<int>> CreateExecutor()
    {
        return new CountMethodExecutor<TEntity>(GetStreamMergeContext());
    }
}
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;


internal class SumAsyncInMemoryMergeEngine<TEntity, TResult> : AbstractMethodEnsureWrapMergeEngine<TResult>
{
    public SumAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }


    protected override IExecutor<RouteQueryResult<TResult>> CreateExecutor()
    {
        return new SumMethodExecutor<TResult>(GetStreamMergeContext());
    }
}
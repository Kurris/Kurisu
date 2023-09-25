using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;


internal class AllAsyncInMemoryMergeEngine<TEntity> : AbstractMethodEnsureMergeEngine<bool>
{
    public AllAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }

    protected override IExecutor<bool> CreateExecutor()
    {
        return new AllMethodExecutor<TEntity>(GetStreamMergeContext());
    }
}
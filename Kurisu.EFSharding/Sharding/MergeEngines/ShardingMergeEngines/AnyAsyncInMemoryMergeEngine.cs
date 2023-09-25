using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class AnyAsyncInMemoryMergeEngine<TEntity> : AbstractMethodEnsureMergeEngine<bool>
{
    public AnyAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }

    protected override IExecutor<bool> CreateExecutor()
    {

        return new AnyMethodExecutor<TEntity>(GetStreamMergeContext());
    }
}
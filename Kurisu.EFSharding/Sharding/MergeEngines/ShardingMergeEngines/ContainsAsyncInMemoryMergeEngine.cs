using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class ContainsAsyncInMemoryMergeEngine<TEntity>: AbstractMethodEnsureMergeEngine<bool>
{
    public ContainsAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }

    protected override IExecutor<bool> CreateExecutor()
    {
        return new ContainsMethodExecutor<TEntity>(GetStreamMergeContext());
    }
}
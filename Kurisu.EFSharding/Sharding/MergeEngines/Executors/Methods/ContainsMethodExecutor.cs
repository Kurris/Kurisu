using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class ContainsMethodExecutor<TEntity> : AbstractMethodExecutor<bool>
{
    public ContainsMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {
        var circuitBreaker = new ContainsCircuitBreaker(GetStreamMergeContext());
        circuitBreaker.Register(() =>
        {
            Cancel();
        });
        return circuitBreaker;
    }

    public override IShardingMerger<bool> GetShardingMerger()
    {
        return ContainsMethodShardingMerger.Instance;
    }

    protected override Task<bool> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        var constantQueryCombineResult = (ConstantQueryCombineResult)GetStreamMergeContext().MergeQueryCompilerContext.GetQueryCombineResult();
        var constantItem = (TEntity)constantQueryCombineResult.GetConstantItem();
        return queryable.As<IQueryable<TEntity>>().ContainsAsync(constantItem, cancellationToken);
    }
}
using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class AllMethodExecutor<TEntity> : AbstractMethodExecutor<bool>
{
    public AllMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {
        var allCircuitBreaker = new AllCircuitBreaker(GetStreamMergeContext());
        allCircuitBreaker.Register(() =>
        {
            Cancel();
        });
        return allCircuitBreaker;
    }

    public override IShardingMerger<bool> GetShardingMerger()
    {
        return AllMethodShardingMerger.Instance;
    }

    protected override Task<bool> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        var allQueryCombineResult = (AllQueryCombineResult)GetStreamMergeContext().MergeQueryCompilerContext.GetQueryCombineResult();
        Expression<Func<TEntity, bool>> allPredicate = x => true;
        var predicate = allQueryCombineResult.GetAllPredicate();
        if (predicate != null)
        {
            allPredicate = (Expression<Func<TEntity, bool>>)predicate;
        }
        return queryable.As<IQueryable<TEntity>>().AllAsync(allPredicate, cancellationToken);
    }
}
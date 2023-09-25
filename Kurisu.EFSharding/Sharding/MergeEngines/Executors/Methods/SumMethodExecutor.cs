using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class SumMethodExecutor<TEntity> : AbstractMethodWrapExecutor<TEntity>
{
    public SumMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {
        var circuitBreaker = new NoTripCircuitBreaker(GetStreamMergeContext());
        circuitBreaker.Register(() =>
        {
            Cancel();
        });
        return circuitBreaker;
    }

    public override IShardingMerger<RouteQueryResult<TEntity>> GetShardingMerger()
    {
        return new SumMethodShardingMerger<TEntity>();
    }

    protected override Task<TEntity> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        var resultType = typeof(TEntity);
        if (!resultType.IsNumericType())
            throw new ShardingCoreException(
                $"not support {GetStreamMergeContext().MergeQueryCompilerContext.GetQueryExpression().ShardingPrint()} result {resultType}");
#if !EFCORE2
        return ShardingEntityFrameworkQueryableExtensions.ExecuteAsync<TEntity, Task<TEntity>>(
            ShardingQueryableMethods.GetSumWithoutSelector(resultType), (IQueryable<TEntity>)queryable,
            (Expression)null, cancellationToken);
#endif
#if EFCORE2
           return ShardingEntityFrameworkQueryableExtensions.ExecuteAsync<TEntity, TEntity>(ShardingQueryableMethods.GetSumWithoutSelector(resultType), (IQueryable<TEntity>)queryable, cancellationToken);
#endif
    }
}
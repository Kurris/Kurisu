using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class MaxMethodExecutor<TEntity,TResult> : AbstractMethodWrapExecutor<TResult>
{
    public MaxMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {

        var circuitBreaker = new AnyElementCircuitBreaker(GetStreamMergeContext());
        circuitBreaker.Register(() =>
        {
            Cancel();
        });
        return circuitBreaker;
    }

    public override IShardingMerger<RouteQueryResult<TResult>> GetShardingMerger()
    {
        return new MaxMethodShardingMerger<TResult>();
    }

    protected override Task<TResult> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {

        var resultType = typeof(TEntity);
        if (!resultType.IsNullableType())
        {
            if (typeof(decimal) == resultType)
            {
                return queryable.As<IQueryable<decimal>>().Select(o => (decimal?)o).MaxAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(float) == resultType)
            {
                return queryable.As<IQueryable<float>>().Select(o => (float?)o).MaxAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(int) == resultType)
            {
                return queryable.As<IQueryable<int>>().Select(o => (int?)o).MaxAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(long) == resultType)
            {
                return queryable.As<IQueryable<long>>().Select(o => (long?)o).MaxAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(double) == resultType)
            {
                return queryable.As<IQueryable<double>>().Select(o => (double?)o).MaxAsync(cancellationToken).As<Task<TResult>>();
            }

            throw new ShardingCoreException($"cant calc max value, type:[{resultType}]");
        }
        else
        {
            return queryable.As<IQueryable<TEntity>>().MaxAsync(cancellationToken).As<Task<TResult>>();
        }
    }
    //private TEntity GetMaxTResult<TInnerSelect>(List<RouteQueryResult<TInnerSelect>> source)
    //{
    //    var routeQueryResults = source.Where(o => o.QueryResult != null).ToList();
    //    if (routeQueryResults.IsEmpty())
    //        throw new InvalidOperationException("Sequence contains no elements.");
    //    var max = routeQueryResults.Max(o => o.QueryResult);

    //    return ConvertMax<TInnerSelect>(max);
    //}

    //private TEntity ConvertMax<TNumber>(TNumber number)
    //{
    //    if (number == null)
    //        return default;
    //    var convertExpr = Expression.Convert(Expression.Constant(number), typeof(TEntity));
    //    return Expression.Lambda<Func<TEntity>>(convertExpr).Compile()();
    //}
}
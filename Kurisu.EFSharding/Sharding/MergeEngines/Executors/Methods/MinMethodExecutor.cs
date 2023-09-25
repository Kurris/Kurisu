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

internal class MinMethodExecutor<TEntity,TResult> : AbstractMethodWrapExecutor<TResult>
{
    public MinMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
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
        return new MinMethodShardingMerger<TResult>();
    }

    protected override Task<TResult> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {

        var resultType = typeof(TEntity);
        if (!resultType.IsNullableType())
        {
            if (typeof(decimal) == resultType)
            {
                return queryable.As<IQueryable<decimal>>().Select(o => (decimal?)o).MinAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(float) == resultType)
            {
                return queryable.As<IQueryable<float>>().Select(o => (float?)o).MinAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(int) == resultType)
            {
                return queryable.As<IQueryable<int>>().Select(o => (int?)o).MinAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(long) == resultType)
            {
                return queryable.As<IQueryable<long>>().Select(o => (long?)o).MinAsync(cancellationToken).As<Task<TResult>>();
            }
            if (typeof(double) == resultType)
            {
                return queryable.As<IQueryable<double>>().Select(o => (double?)o).MinAsync(cancellationToken).As<Task<TResult>>();
            }

            throw new ShardingCoreException($"cant calc min value, type:[{resultType}]");
        }
        else
        {
            return queryable.As<IQueryable<TEntity>>().MinAsync(cancellationToken).As<Task<TResult>>();
        }
    }
}
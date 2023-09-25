using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingExecutors;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class MinAsyncInMemoryMergeEngine<TEntity, TResult> : AbstractBaseMergeEngine, IEnsureMergeResult<TResult>
{
    public MinAsyncInMemoryMergeEngine(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public TResult MergeResult()
    {
        return MergeResultAsync().WaitAndUnwrapException(false);
    }


    public async Task<TResult> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var resultType = typeof(TEntity);
        if (!resultType.IsNullableType())
        {
            if (typeof(decimal) == resultType)
            {
                var result = await ExecuteAsync<decimal?>(cancellationToken);
                return ConvertNumber(result);
            }

            if (typeof(float) == resultType)
            {
                var result = await ExecuteAsync<float?>(cancellationToken);
                return ConvertNumber(result);
            }

            if (typeof(int) == resultType)
            {
                var result = await ExecuteAsync<int?>(cancellationToken);
                return ConvertNumber(result);
            }

            if (typeof(long) == resultType)
            {
                var result = await ExecuteAsync<long?>(cancellationToken);
                return ConvertNumber(result);
            }

            if (typeof(double) == resultType)
            {
                var result = await ExecuteAsync<double?>(cancellationToken);
                return ConvertNumber(result);
            }

            throw new ShardingCoreException($"cant calc min value, type:[{resultType}]");
        }
        else
        {
            var result = await ExecuteAsync<TResult>(cancellationToken);
            return result;
        }
    }

    private TResult ConvertNumber<TNumber>(TNumber number)
    {
        if (number == null)
            return default;
        var convertExpr = Expression.Convert(Expression.Constant(number), typeof(TResult));
        return Expression.Lambda<Func<TResult>>(convertExpr).Compile()();
    }

    private async Task<TR> ExecuteAsync<TR>(CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GetStreamMergeContext().TryPrepareExecuteContinueQuery(() => default(TR), out var tr))
        {
            return tr;
        }

        var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
        var executor = CreateExecutor<TR>();
        var result = await ShardingExecutor.ExecuteAsync<RouteQueryResult<TR>>(GetStreamMergeContext(), executor, true, defaultSqlRouteUnits, cancellationToken).ConfigureAwait(false);
        return result.QueryResult;
    }

    protected IExecutor<RouteQueryResult<TR>> CreateExecutor<TR>()
    {
        var resultType = typeof(TEntity);
        if (!resultType.IsNullableType())
        {
            if (typeof(decimal) == resultType)
            {
                return new MinMethodExecutor<TEntity, decimal?>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
            }

            if (typeof(float) == resultType)
            {
                return new MinMethodExecutor<TEntity, float?>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
            }

            if (typeof(int) == resultType)
            {
                return new MinMethodExecutor<TEntity, int?>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
            }

            if (typeof(long) == resultType)
            {
                return new MinMethodExecutor<TEntity, long?>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
            }

            if (typeof(double) == resultType)
            {
                return new MinMethodExecutor<TEntity, double?>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
            }

            throw new ShardingCoreException($"cant calc min value, type:[{resultType}]");
        }
        else
        {
            return new MinMethodExecutor<TEntity, TEntity>(GetStreamMergeContext()) as IExecutor<RouteQueryResult<TR>>;
        }
    }
}
using Kurisu.EFSharding.Sharding.Enumerators.AggregateExtensions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingExecutors;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Averages;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

internal class AverageAsyncInMemoryMergeEngine<TEntity, TResult, TSelect> : AbstractBaseMergeEngine, IEnsureMergeResult<TResult>
{
    public AverageAsyncInMemoryMergeEngine(StreamMergeContext streamStreamMergeContext) : base(streamStreamMergeContext)
    {
    }


    protected IExecutor<RouteQueryResult<AverageResult<TSelect>>> CreateExecutor()
    {
        return new AverageMethodWrapExecutor<TSelect>(GetStreamMergeContext());
    }

    public TResult MergeResult()
    {
        return MergeResultAsync().WaitAndUnwrapException(false);
    }

    public async Task<TResult> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GetStreamMergeContext().TryPrepareExecuteContinueQuery(() => default(TResult), out var emptyQueryEnumerator))
        {
            return emptyQueryEnumerator;
        }

        var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
        var executor = CreateExecutor();
        var result = await ShardingExecutor.ExecuteAsync(GetStreamMergeContext(), executor, true, defaultSqlRouteUnits, cancellationToken).ConfigureAwait(false);
        var sum = result.QueryResult.Sum;
        var count = result.QueryResult.Count;

        return AggregateExtension.AverageConstant<TSelect, long, TResult>(sum, count);
    }
}
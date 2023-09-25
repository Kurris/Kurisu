using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingExecutors;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;

internal abstract class AbstractMethodEnsureWrapMergeEngine<TResult> : AbstractBaseMergeEngine,IEnsureMergeResult<TResult>
{

    protected AbstractMethodEnsureWrapMergeEngine(StreamMergeContext streamMergeContext):base(streamMergeContext)
    {
    }

    protected abstract IExecutor<RouteQueryResult<TResult>> CreateExecutor();
    public virtual TResult MergeResult()
    {
        return MergeResultAsync().WaitAndUnwrapException(false);
    }

    public  async Task<TResult> MergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GetStreamMergeContext().TryPrepareExecuteContinueQuery(() => default(TResult),out var emptyQueryEnumerator))
        {
            return emptyQueryEnumerator;
        }
        var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
        var executor = CreateExecutor();
        var result =await ShardingExecutor.ExecuteAsync<RouteQueryResult<TResult>>(GetStreamMergeContext(),
            executor, true, defaultSqlRouteUnits, cancellationToken).ConfigureAwait(false);
        return result.QueryResult;
    }
}
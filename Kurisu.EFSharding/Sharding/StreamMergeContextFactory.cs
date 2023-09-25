using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.MergeContexts;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding;

internal class StreamMergeContextFactory : IStreamMergeContextFactory
{
    private readonly IQueryableParseEngine _queryableParseEngine;
    private readonly IQueryableRewriteEngine _queryableRewriteEngine;
    private readonly IQueryableOptimizeEngine _queryableOptimizeEngine;

    public StreamMergeContextFactory(IQueryableParseEngine queryableParseEngine,
        IQueryableRewriteEngine queryableRewriteEngine,
        IQueryableOptimizeEngine queryableOptimizeEngine)
    {
        _queryableParseEngine = queryableParseEngine;
        _queryableRewriteEngine = queryableRewriteEngine;
        _queryableOptimizeEngine = queryableOptimizeEngine;
    }

    public StreamMergeContext Create(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        var parseResult = _queryableParseEngine.Parse(mergeQueryCompilerContext);

        var rewriteResult = _queryableRewriteEngine.GetRewriteQueryable(mergeQueryCompilerContext, parseResult);
        var optimizeResult = _queryableOptimizeEngine.Optimize(mergeQueryCompilerContext, parseResult, rewriteResult);
        CheckMergeContext(mergeQueryCompilerContext, parseResult, rewriteResult, optimizeResult);
        return new StreamMergeContext(mergeQueryCompilerContext, parseResult, rewriteResult, optimizeResult);
    }

    private void CheckMergeContext(IMergeQueryCompilerContext mergeQueryCompilerContext, IParseResult parseResult, IRewriteResult rewriteResult, IOptimizeResult optimizeResult)
    {
        var paginationContext = parseResult.GetPaginationContext();
        if (paginationContext.Skip is < 0)
        {
            throw new ShardingCoreException($"queryable skip should >= 0");
        }

        if (paginationContext.Take is < 0)
        {
            throw new ShardingCoreException($"queryable take should >= 0");
        }

        if (!mergeQueryCompilerContext.IsEnumerableQuery())
        {
            if ((nameof(Enumerable.Last) == mergeQueryCompilerContext.GetQueryMethodName() || nameof(Enumerable.LastOrDefault) == mergeQueryCompilerContext.GetQueryMethodName()) && parseResult.GetOrderByContext().PropertyOrders.IsEmpty())
            {
                throw new InvalidOperationException(
                    "Queries performing 'LastOrDefault' operation must have a deterministic sort order. Rewrite the query to apply an 'OrderBy' operation on the sequence before calling 'LastOrDefault'");
            }
        }
    }
}
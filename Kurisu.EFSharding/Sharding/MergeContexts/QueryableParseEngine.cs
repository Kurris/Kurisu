using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Sharding.Visitors;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

public class QueryableParseEngine:IQueryableParseEngine
{
    public IParseResult Parse(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        var isEnumerableQuery = mergeQueryCompilerContext.IsEnumerableQuery();
        string queryMethodName = mergeQueryCompilerContext.GetQueryMethodName();
        var combineQueryable = mergeQueryCompilerContext.GetQueryCombineResult().GetCombineQueryable();
        var queryableExtraDiscoverVisitor = new QueryableExtraDiscoverVisitor(mergeQueryCompilerContext);
        queryableExtraDiscoverVisitor.Visit(combineQueryable.Expression);
        return new ParseResult(queryableExtraDiscoverVisitor.GetPaginationContext(),
            queryableExtraDiscoverVisitor.GetOrderByContext(), queryableExtraDiscoverVisitor.GetSelectContext(),
            queryableExtraDiscoverVisitor.GetGroupByContext(),isEnumerableQuery,queryMethodName);
    }
}
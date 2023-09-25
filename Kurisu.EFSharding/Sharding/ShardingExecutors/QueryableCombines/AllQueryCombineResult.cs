using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class AllQueryCombineResult: QueryCombineResult
{
    private readonly LambdaExpression _allPredicate;

    public AllQueryCombineResult(LambdaExpression allPredicate,IQueryable queryable,IQueryCompilerContext queryCompilerContext) : base(queryable, queryCompilerContext)
    {
        _allPredicate = allPredicate;
    }

    public LambdaExpression GetAllPredicate()
    {
        return _allPredicate;
    }
}
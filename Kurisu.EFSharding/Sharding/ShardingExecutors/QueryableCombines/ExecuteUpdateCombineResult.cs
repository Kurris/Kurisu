using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class ExecuteUpdateCombineResult: QueryCombineResult
{
    private readonly LambdaExpression _setPropertyCalls;

    public ExecuteUpdateCombineResult(LambdaExpression setPropertyCalls,IQueryable queryable,IQueryCompilerContext queryCompilerContext) : base(queryable, queryCompilerContext)
    {
        _setPropertyCalls = setPropertyCalls;
    }

    public LambdaExpression GetSetPropertyCalls()
    {
        return _setPropertyCalls;
    }
}
using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class AllQueryableCombine : AbstractQueryableCombine
{
    public override IQueryable DoCombineQueryable(IQueryable queryable, Expression secondExpression, IQueryCompilerContext queryCompilerContext)
    {

        return queryable;
    }

    public override QueryCombineResult GetDefaultQueryCombineResult(IQueryable queryable, Expression secondExpression, IQueryCompilerContext queryCompilerContext)
    {
        LambdaExpression allPredicate = null;
        if (secondExpression is UnaryExpression where && where.Operand is LambdaExpression lambdaExpression)
        {
            allPredicate = lambdaExpression;
        }

        return new AllQueryCombineResult(allPredicate,queryable, queryCompilerContext);
    }
}
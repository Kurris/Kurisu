using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class ConstantQueryableCombine:AbstractQueryableCombine
{
    public override IQueryable DoCombineQueryable(IQueryable queryable, Expression secondExpression, IQueryCompilerContext queryCompilerContext)
    {
        if (!(secondExpression is ConstantExpression))
        {
            throw new ShardingCoreInvalidOperationException(queryCompilerContext.GetQueryExpression().ShardingPrint());
        }

        return queryable;
    }

    public override QueryCombineResult GetDefaultQueryCombineResult(IQueryable queryable, Expression secondExpression, IQueryCompilerContext queryCompilerContext)
    {
        if (!(secondExpression is ConstantExpression constantExpression))
        {

            throw new ShardingCoreException($"not found constant {queryCompilerContext.GetQueryExpression().ShardingPrint()}");
        }
        var constantItem = constantExpression.Value;

        return new ConstantQueryCombineResult(constantItem, queryable, queryCompilerContext);
    }
}
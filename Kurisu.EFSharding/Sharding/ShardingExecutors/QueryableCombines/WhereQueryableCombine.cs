using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class WhereQueryableCombine: AbstractQueryableCombine
{
    public override IQueryable DoCombineQueryable(IQueryable queryable, Expression secondExpression, IQueryCompilerContext queryCompilerContext)
    {
        if (secondExpression is UnaryExpression where && where.Operand is LambdaExpression lambdaExpression )
        {
            MethodCallExpression whereCallExpression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Where),
                new Type[] { queryable.ElementType },
                queryable.Expression,lambdaExpression
            );
            return queryable.Provider.CreateQuery(whereCallExpression);
        }

        throw new ShardingCoreInvalidOperationException(queryCompilerContext.GetQueryExpression().ShardingPrint());
    }
}
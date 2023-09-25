using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class SelectQueryableCombine:AbstractQueryableCombine
{
    public override IQueryable DoCombineQueryable(IQueryable queryable, Expression secondExpression,
        IQueryCompilerContext queryCompilerContext)
    {
        if (secondExpression != null)
        {
            if (secondExpression is UnaryExpression unaryExpression && unaryExpression.Operand is LambdaExpression lambdaExpression)
            {
                MethodCallExpression selectCallExpression = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Select),
                    new Type[] { queryable.ElementType, lambdaExpression.Body.Type },
                    queryable.Expression, lambdaExpression
                );
                return queryable.Provider.CreateQuery(selectCallExpression);
            }

            throw new ShardingCoreException($"expression is not selector:{queryCompilerContext.GetQueryExpression().ShardingPrint()}");
        }
        return queryable;
    }
}
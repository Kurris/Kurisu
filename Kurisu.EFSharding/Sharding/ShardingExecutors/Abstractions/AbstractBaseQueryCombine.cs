using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

public abstract class AbstractBaseQueryCombine:IQueryableCombine
{
    public abstract QueryCombineResult Combine(IQueryCompilerContext queryCompilerContext);
    private bool IsEnumerableQuery(IQueryCompilerContext queryCompilerContext)
    {

        return queryCompilerContext.GetQueryExpression().Type
            .HasImplementedRawGeneric(typeof(IQueryable<>));
    }

    private Type GetEnumerableQueryEntityType(IQueryCompilerContext queryCompilerContext)
    {
        return queryCompilerContext.GetQueryExpression().Type.GetGenericArguments()[0];
    }

    protected Type GetQueryableEntityType(IQueryCompilerContext queryCompilerContext)
    {

        if (queryCompilerContext.IsEnumerableQuery())
        {
            return GetEnumerableQueryEntityType(queryCompilerContext);
        }
        else
        {
            return (queryCompilerContext.GetQueryExpression() as MethodCallExpression)
                .GetQueryEntityType();
        }
    }
}
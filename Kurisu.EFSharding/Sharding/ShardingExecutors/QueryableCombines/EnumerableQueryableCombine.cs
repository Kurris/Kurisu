using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class EnumerableQueryableCombine : AbstractBaseQueryCombine
{
    public override QueryCombineResult Combine(IQueryCompilerContext queryCompilerContext)
    {

        Type type = typeof(EnumerableQuery<>);
        type = type.MakeGenericType(GetQueryableEntityType(queryCompilerContext));
        var queryable = (IQueryable)Activator.CreateInstance(type, queryCompilerContext.GetQueryExpression());
        return new QueryCombineResult(queryable, queryCompilerContext);

    }
}
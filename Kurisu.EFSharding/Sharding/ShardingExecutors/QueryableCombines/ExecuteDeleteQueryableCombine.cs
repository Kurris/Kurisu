using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class ExecuteDeleteQueryableCombine: AbstractQueryableCombine
{
    public override IQueryable DoCombineQueryable(IQueryable queryable, Expression secondExpression,
        IQueryCompilerContext queryCompilerContext)
    {
        return queryable;
    }
}
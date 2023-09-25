using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class ConstantQueryCombineResult : QueryCombineResult
{
    private readonly object _constantItem;

    public ConstantQueryCombineResult(object constantItem, IQueryable queryable, IQueryCompilerContext queryCompilerContext) : base(queryable, queryCompilerContext)
    {
        _constantItem = constantItem;
    }

    public object GetConstantItem()
    {
        return _constantItem;
    }
}
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

public class QueryCombineResult
{
    private readonly IQueryable _queryable;
    private readonly IQueryCompilerContext _queryCompilerContext;

    public QueryCombineResult(IQueryable queryable,IQueryCompilerContext queryCompilerContext)
    {
        _queryable = queryable;
        _queryCompilerContext = queryCompilerContext;
    }
    public IQueryable GetCombineQueryable()
    {
        return _queryable;
    }

    public IQueryCompilerContext GetQueryCompilerContext()
    {
        return _queryCompilerContext;
    }
}
using System.Linq.Expressions;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors;

public class QueryCompilerExecutor
{
    private readonly IQueryCompiler _queryCompiler;
    private readonly Expression _queryExpression;
    private readonly Expression _originalQueryExpression;

    public QueryCompilerExecutor(DbContext dbContext,Expression queryExpression)
    {
        _queryCompiler = dbContext.GetService<IQueryCompiler>();
        _originalQueryExpression = queryExpression;
        _queryExpression = queryExpression.ReplaceDbContextExpression(dbContext);
    }

    public IQueryCompiler GetQueryCompiler()
    {
        return _queryCompiler;
    }

    public Expression GetReplaceQueryExpression()
    {
        return _queryExpression;
    }
}
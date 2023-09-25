using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.EFCores;

/// <summary>
/// 当前查询编译拦截
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class ShardingQueryCompiler : QueryCompiler, IShardingDbContextAvailable
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IShardingCompilerExecutor _shardingCompilerExecutor;

    public ShardingQueryCompiler(IShardingRuntimeContext shardingRuntimeContext, IQueryContextFactory queryContextFactory, ICompiledQueryCache compiledQueryCache, ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator, IDatabase database, IDiagnosticsLogger<DbLoggerCategory.Query> logger, ICurrentDbContext currentContext, IEvaluatableExpressionFilter evaluatableExpressionFilter, IModel model)
        : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, evaluatableExpressionFilter, model)
    {
        _shardingDbContext = currentContext.Context as IShardingDbContext ?? throw new ShardingCoreException("db context operator is not IShardingDbContext");
        _shardingCompilerExecutor = shardingRuntimeContext.GetShardingCompilerExecutor();
    }


    public override TResult Execute<TResult>(Expression query)
    {
        return _shardingCompilerExecutor.Execute<TResult>(_shardingDbContext, query);
    }


    public override TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken = new())
    {
        return _shardingCompilerExecutor.ExecuteAsync<TResult>(_shardingDbContext, query, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    public override Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public override Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query)
    {
        throw new NotImplementedException();
    }


    public IShardingDbContext GetShardingDbContext()
    {
        return _shardingDbContext;
    }
}
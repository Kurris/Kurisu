using System.Linq.Expressions;
using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors;

/// <summary>
/// 默认的分片编译执行者
/// </summary>
public class DefaultShardingCompilerExecutor : IShardingCompilerExecutor
{
    private readonly ILogger<DefaultShardingCompilerExecutor> _logger;
    private readonly IShardingTrackQueryExecutor _shardingTrackQueryExecutor;
    private readonly IQueryCompilerContextFactory _queryCompilerContextFactory;
    private readonly IPrepareParser _prepareParser;
    private readonly IShardingRouteManager _shardingRouteManager;

    public DefaultShardingCompilerExecutor(
        IShardingTrackQueryExecutor shardingTrackQueryExecutor, IQueryCompilerContextFactory queryCompilerContextFactory, IPrepareParser prepareParser,
        IShardingRouteManager shardingRouteManager, ILogger<DefaultShardingCompilerExecutor> logger)
    {
        _shardingTrackQueryExecutor = shardingTrackQueryExecutor;
        _queryCompilerContextFactory = queryCompilerContextFactory;
        _prepareParser = prepareParser;
        _shardingRouteManager = shardingRouteManager;
        _logger = logger;
    }

    public TResult Execute<TResult>(IShardingDbContext shardingDbContext, Expression query)
    {
        //预解析表达式
        var prepareParseResult = _prepareParser.Parse(shardingDbContext, query);
        _logger.LogDebug($"Compile parameter:{prepareParseResult}");
        using (new CustomerQueryScope(prepareParseResult, _shardingRouteManager))
        {
            var queryCompilerContext = _queryCompilerContextFactory.Create(prepareParseResult);
            return _shardingTrackQueryExecutor.Execute<TResult>(queryCompilerContext);
        }
    }

    public TResult ExecuteAsync<TResult>(IShardingDbContext shardingDbContext, Expression query,
        CancellationToken cancellationToken = new())
    {
        //预解析表达式
        var prepareParseResult = _prepareParser.Parse(shardingDbContext, query);
        _logger.LogDebug($"compile parameter:{prepareParseResult}");

        using (new CustomerQueryScope(prepareParseResult, _shardingRouteManager))
        {
            var queryCompilerContext = _queryCompilerContextFactory.Create(prepareParseResult);
            return  _shardingTrackQueryExecutor.ExecuteAsync<TResult>(queryCompilerContext);
        }
    }
}
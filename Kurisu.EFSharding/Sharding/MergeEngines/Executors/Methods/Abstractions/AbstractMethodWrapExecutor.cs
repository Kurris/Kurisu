using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;


internal abstract class AbstractMethodWrapExecutor<TResult> : AbstractExecutor<RouteQueryResult<TResult>>
{
    protected AbstractMethodWrapExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override async Task<ShardingMergeResult<RouteQueryResult<TResult>>> ExecuteUnitAsync(SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();
        var dataSourceName = sqlExecutorUnit.RouteUnit.DatasourceName;
        var routeResult = sqlExecutorUnit.RouteUnit.TableRouteResult;

        var shardingDbContext = streamMergeContext.CreateDbContext(sqlExecutorUnit.RouteUnit);
        var newQueryable = GetStreamMergeContext().GetReWriteQueryable()
            .ReplaceDbContextQueryable(shardingDbContext);

        var queryResult = await EFCoreQueryAsync(newQueryable, cancellationToken);
        var routeQueryResult = new RouteQueryResult<TResult>(dataSourceName, routeResult, queryResult);
        await streamMergeContext.DbContextDisposeAsync(shardingDbContext);
        return new ShardingMergeResult<RouteQueryResult<TResult>>(null, routeQueryResult);
    }

    protected abstract Task<TResult> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken());
}
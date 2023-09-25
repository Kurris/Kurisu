using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;

internal abstract class AbstractMethodExecutor<TResult> : AbstractExecutor<TResult>
{
    protected AbstractMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override async Task<ShardingMergeResult<TResult>> ExecuteUnitAsync(SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();

        var shardingDbContext = streamMergeContext.CreateDbContext(sqlExecutorUnit.RouteUnit);
        var newQueryable = GetStreamMergeContext().GetReWriteQueryable()
            .ReplaceDbContextQueryable(shardingDbContext);

        var queryResult = await EFCoreQueryAsync(newQueryable, cancellationToken);
        await streamMergeContext.DbContextDisposeAsync(shardingDbContext);
        return new ShardingMergeResult<TResult>(null, queryResult);
    }

    protected abstract Task<TResult> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken());
}
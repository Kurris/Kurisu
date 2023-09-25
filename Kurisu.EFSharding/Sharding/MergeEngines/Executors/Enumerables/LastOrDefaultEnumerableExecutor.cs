using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;

internal class LastOrDefaultEnumerableExecutor<TResult> : AbstractEnumerableExecutor<TResult>
{
    private readonly IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> _shardingMerger;
    private readonly IQueryable<TResult> _queryable;
    private readonly bool _async;

    public LastOrDefaultEnumerableExecutor(StreamMergeContext streamMergeContext, IQueryable<TResult> queryable,
        bool async) : base(streamMergeContext)
    {
        _queryable = queryable;
        _async = async;
        _shardingMerger = new LastOrDefaultEnumerableShardingMerger<TResult>(GetStreamMergeContext(), async);
    }


    protected override async Task<ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>> ExecuteUnitAsync0(
        SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();

        var shardingDbContext = streamMergeContext.CreateDbContext(sqlExecutorUnit.RouteUnit);
        var newQueryable = (IQueryable<TResult>)_queryable.ReplaceDbContextQueryable(shardingDbContext);

        var streamMergeAsyncEnumerator = await AsyncParallelEnumerator(newQueryable, _async, cancellationToken);
        return new ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>(shardingDbContext,
            streamMergeAsyncEnumerator);
    }

    public override IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> GetShardingMerger()
    {
        return _shardingMerger;
    }
}
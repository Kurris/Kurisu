using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;

internal class SequenceEnumerableExecutor<TResult> : AbstractEnumerableExecutor<TResult>
{
    private readonly IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> _shardingMerger;
    private readonly IQueryable<TResult> _noPaginationQueryable;
    private readonly bool _async;

    public SequenceEnumerableExecutor(StreamMergeContext streamMergeContext, bool async) : base(streamMergeContext)
    {
        _async = async;
        _noPaginationQueryable = streamMergeContext.GetOriginalQueryable().RemoveSkip().RemoveTake().As<IQueryable<TResult>>();
        _shardingMerger = new SequenceEnumerableShardingMerger<TResult>(streamMergeContext, async);
    }

    protected override async Task<ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>> ExecuteUnitAsync0(SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();
        var sqlSequenceRouteUnit = sqlExecutorUnit.RouteUnit.As<SqlSequenceRouteUnit>();
        var sequenceResult = sqlSequenceRouteUnit.SequenceResult;
        var shardingDbContext = streamMergeContext.CreateDbContext(sqlSequenceRouteUnit);
        var newQueryable = _noPaginationQueryable
            .Skip(sequenceResult.Skip)
            .Take(sequenceResult.Take)
            .ReplaceDbContextQueryable(shardingDbContext)
            .As<IQueryable<TResult>>();
        var streamMergeAsyncEnumerator = await AsyncParallelEnumerator(newQueryable, _async, cancellationToken);
        return new ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>(shardingDbContext, streamMergeAsyncEnumerator);
    }

    public override IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> GetShardingMerger()
    {
        return _shardingMerger;
    }
}
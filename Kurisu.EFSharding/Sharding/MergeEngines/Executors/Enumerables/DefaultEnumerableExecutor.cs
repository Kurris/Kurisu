using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;

internal class DefaultEnumerableExecutor<TResult> : AbstractEnumerableExecutor<TResult>
{
    // private readonly IStreamMergeCombine _streamMergeCombine;
    private readonly IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> _shardingMerger;
    private readonly bool _async;

    public DefaultEnumerableExecutor(StreamMergeContext streamMergeContext, bool async) : base(streamMergeContext)
    {
        // _streamMergeCombine = streamMergeCombine;
        _async = async;
        _shardingMerger = new DefaultEnumerableShardingMerger<TResult>(streamMergeContext,async);
    }
    //
    // protected override IStreamMergeCombine GetStreamMergeCombine()
    // {
    //     return _streamMergeCombine;
    // }

    // public override IStreamMergeAsyncEnumerator<TResult> CombineInMemoryStreamMergeAsyncEnumerator(
    //     IStreamMergeAsyncEnumerator<TResult>[] streamsAsyncEnumerators)
    // {
    //     if (GetStreamMergeContext().IsPaginationQuery())
    //         return new PaginationStreamMergeAsyncEnumerator<TResult>(GetStreamMergeContext(), streamsAsyncEnumerators, 0, GetStreamMergeContext().GetPaginationReWriteTake());//内存聚合分页不可以直接获取skip必须获取skip+take的数目
    //     return base.CombineInMemoryStreamMergeAsyncEnumerator(streamsAsyncEnumerators);
    // }

    protected override async Task<ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>> ExecuteUnitAsync0(SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();
        var shardingDbContext = streamMergeContext.CreateDbContext(sqlExecutorUnit.RouteUnit);
        var newQueryable = (IQueryable<TResult>)streamMergeContext.GetReWriteQueryable()
            .ReplaceDbContextQueryable(shardingDbContext);

        var streamMergeAsyncEnumerator = await AsyncParallelEnumerator(newQueryable, _async, cancellationToken);
        return new ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>(shardingDbContext, streamMergeAsyncEnumerator);
    }

    public override IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> GetShardingMerger()
    {
        return _shardingMerger;
    }
}
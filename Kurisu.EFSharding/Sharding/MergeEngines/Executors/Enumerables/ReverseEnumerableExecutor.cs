using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;


internal class ReverseEnumerableExecutor<TResult> : AbstractEnumerableExecutor<TResult>
{
    private readonly IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> _shardingMerger;
    private readonly IOrderedQueryable<TResult> _reverseOrderQueryable;
    private readonly bool _async;

    public ReverseEnumerableExecutor(StreamMergeContext streamMergeContext, IOrderedQueryable<TResult> reverseOrderQueryable, bool async) : base(streamMergeContext)
    {
        _reverseOrderQueryable = reverseOrderQueryable;
        _async = async;
        _shardingMerger = new ReverseEnumerableShardingMerger<TResult>(streamMergeContext, async);
    }

    protected override  async Task<ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>> ExecuteUnitAsync0(SqlExecutorUnit sqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken())
    {
        var streamMergeContext = GetStreamMergeContext();

        var shardingDbContext = streamMergeContext.CreateDbContext(sqlExecutorUnit.RouteUnit);
        var newQueryable = _reverseOrderQueryable
            .ReplaceDbContextQueryable(shardingDbContext).As<IQueryable<TResult>>();
        var streamMergeAsyncEnumerator = await AsyncParallelEnumerator(newQueryable, _async, cancellationToken);
        return new ShardingMergeResult<IStreamMergeAsyncEnumerator<TResult>>(shardingDbContext,
            streamMergeAsyncEnumerator);
    }

    public override IShardingMerger<IStreamMergeAsyncEnumerator<TResult>> GetShardingMerger()
    {
        return _shardingMerger;
    }
}
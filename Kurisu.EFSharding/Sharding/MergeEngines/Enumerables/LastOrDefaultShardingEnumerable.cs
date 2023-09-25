using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class LastOrDefaultShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    public LastOrDefaultShardingEnumerable(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        var streamMergeContext = GetStreamMergeContext();
        var skip = streamMergeContext.Skip;
        streamMergeContext.ReverseOrder();
        streamMergeContext.ReSetSkip(0);
        var reTake = skip.GetValueOrDefault() + 1;
        streamMergeContext.ReSetTake(reTake);
        var newQueryable = (IQueryable<TEntity>)streamMergeContext.GetReWriteQueryable().RemoveSkip().RemoveTake().RemoveAnyOrderBy().OrderWithExpression(streamMergeContext.Orders).ReTake(reTake);

        return new LastOrDefaultEnumerableExecutor<TEntity>(GetStreamMergeContext(),newQueryable, async);
    }
}
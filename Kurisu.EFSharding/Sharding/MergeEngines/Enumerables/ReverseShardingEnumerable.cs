using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class ReverseShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    private readonly long _total;

    public ReverseShardingEnumerable(StreamMergeContext streamMergeContext, long total) : base(streamMergeContext)
    {
        _total = total;
    }

    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        var noPaginationNoOrderQueryable = GetStreamMergeContext().GetOriginalQueryable().RemoveSkip().RemoveTake()
            .RemoveAnyOrderBy().As<IQueryable<TEntity>>();
        var skip = GetStreamMergeContext().Skip.GetValueOrDefault();
        var take = GetStreamMergeContext().Take.HasValue ? GetStreamMergeContext().Take.Value : (_total - skip);
        if (take > int.MaxValue)
            throw new ShardingCoreException($"not support take more than {int.MaxValue}");
        var realSkip = _total - take - skip;
        GetStreamMergeContext().ReSetSkip((int)realSkip);
        GetStreamMergeContext().ReverseOrder();
        var reverseOrderQueryable = noPaginationNoOrderQueryable.Take((int)realSkip + (int)take)
            .OrderWithExpression(GetStreamMergeContext().Orders);
        return new ReverseEnumerableExecutor<TEntity>(GetStreamMergeContext(),
            reverseOrderQueryable, async);
    }
}
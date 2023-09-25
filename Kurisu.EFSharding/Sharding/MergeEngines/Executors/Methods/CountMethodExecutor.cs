using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class CountMethodExecutor<TEntity> : AbstractMethodWrapExecutor<int>
{
    public CountMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {
        var circuitBreaker = new NoTripCircuitBreaker(GetStreamMergeContext());
        circuitBreaker.Register(() =>
        {
            Cancel();
        });
        return circuitBreaker;
    }

    public override IShardingMerger<RouteQueryResult<int>> GetShardingMerger()
    {
        return new CountMethodShardingMerger(GetStreamMergeContext());
    }

    protected override Task<int> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        return queryable.As<IQueryable<TEntity>>().CountAsync(cancellationToken);
    }
}
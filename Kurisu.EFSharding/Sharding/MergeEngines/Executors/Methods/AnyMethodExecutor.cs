using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;

internal class AnyMethodExecutor<TEntity> : AbstractMethodExecutor<bool>
{
    public AnyMethodExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    public override ICircuitBreaker CreateCircuitBreaker()
    {
        var anyCircuitBreaker = new AnyCircuitBreaker(GetStreamMergeContext());
        anyCircuitBreaker.Register(() =>
        {
            Cancel();
        });
        return anyCircuitBreaker;
    }

    public override IShardingMerger<bool> GetShardingMerger()
    {
        return AnyMethodShardingMerger.Instance;
    }

    protected override Task<bool> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        return queryable.As<IQueryable<TEntity>>().AnyAsync(cancellationToken);
    }
}
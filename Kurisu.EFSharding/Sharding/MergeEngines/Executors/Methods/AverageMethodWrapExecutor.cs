using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Averages;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Methods;


internal class AverageMethodWrapExecutor<TSelect> : AbstractMethodWrapExecutor<AverageResult<TSelect>>
{
    public AverageMethodWrapExecutor(StreamMergeContext streamMergeContext) : base(streamMergeContext)
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

    public override IShardingMerger<RouteQueryResult<AverageResult<TSelect>>> GetShardingMerger()
    {
        return new AverageMethodShardingMerger<TSelect>();
    }

    protected override async Task<AverageResult<TSelect>> EFCoreQueryAsync(IQueryable queryable, CancellationToken cancellationToken = new CancellationToken())
    {
        var count = 0L;
        TSelect sum = default;
        var newQueryable = ((IQueryable<TSelect>)queryable);
        var r = await newQueryable.GroupBy(o => 1).BuildExpression().FirstOrDefaultAsync(cancellationToken);
        if (r != null)
        {
            count = r.Item1;
            sum = r.Item2;
        }
        if (count <= 0)
        {
            return default;
        }
        return new AverageResult<TSelect>(sum, count);
    }
}
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Averages;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Sharding.Enumerators.AggregateExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class AverageMethodShardingMerger<TSelect> : IShardingMerger<RouteQueryResult<AverageResult<TSelect>>>
{
    public RouteQueryResult<AverageResult<TSelect>> StreamMerge(
        List<RouteQueryResult<AverageResult<TSelect>>> parallelResults)
    {
        if (parallelResults.IsEmpty())
            throw new InvalidOperationException("Sequence contains no elements.");
        var queryable = parallelResults.Where(o => o.HasQueryResult()).Select(o => new
        {
            Sum = o.QueryResult.Sum,
            Count = o.QueryResult.Count
        }).AsQueryable();
        var sum = queryable.SumByPropertyName<TSelect>(nameof(AverageResult<object>.Sum));
        var count = queryable.Sum(o => o.Count);
        return new RouteQueryResult<AverageResult<TSelect>>(null, null,
            new AverageResult<TSelect>(sum , count));
    }

    public void InMemoryMerge(List<RouteQueryResult<AverageResult<TSelect>>> beforeInMemoryResults,
        List<RouteQueryResult<AverageResult<TSelect>>> parallelResults)
    {
        beforeInMemoryResults.AddRange(parallelResults);
    }
}


// var resultList = await base.ExecuteAsync<AverageResult<TSelect>>(cancellationToken);
// if (resultList.IsEmpty())
//     throw new InvalidOperationException("Sequence contains no elements.");
// var queryable = resultList.Where(o=>o.HasQueryResult()).Select(o => new
// {
//     Sum = o.QueryResult.Sum,
//     Count = o.QueryResult.Count
// }).AsQueryable();
// var sum = queryable.SumByPropertyName<TSelect>(nameof(AverageResult<object>.Sum));
// var count = queryable.Sum(o => o.Count);
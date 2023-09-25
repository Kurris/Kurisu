using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class MaxMethodShardingMerger<TResult> : IShardingMerger<RouteQueryResult<TResult>>
{
    public RouteQueryResult<TResult> StreamMerge(List<RouteQueryResult<TResult>> parallelResults)
    {
        var routeQueryResults = parallelResults.Where(o => o.HasQueryResult()).ToList();
        if (routeQueryResults.IsEmpty())
            throw new InvalidOperationException("Sequence contains no elements.");
        var min = routeQueryResults.Max(o => o.QueryResult);
        return new RouteQueryResult<TResult>(null, null, min);
    }

    public void InMemoryMerge(List<RouteQueryResult<TResult>> beforeInMemoryResults,
        List<RouteQueryResult<TResult>> parallelResults)
    {
        beforeInMemoryResults.AddRange(parallelResults);
    }
}
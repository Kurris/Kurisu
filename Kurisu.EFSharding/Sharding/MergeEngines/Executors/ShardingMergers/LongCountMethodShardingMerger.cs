using Kurisu.EFSharding.Core.ShardingPage.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class LongCountMethodShardingMerger:IShardingMerger<RouteQueryResult<long>>
{
    private readonly IShardingPageManager _shardingPageManager;

    public LongCountMethodShardingMerger(StreamMergeContext streamMergeContext)
    {
        _shardingPageManager =streamMergeContext.ShardingRuntimeContext.GetShardingPageManager();
    }
    public RouteQueryResult<long> StreamMerge(List<RouteQueryResult<long>> parallelResults)
    {

        if (_shardingPageManager.Current != null)
        {
            long r = 0;
            foreach (var routeQueryResult in parallelResults)
            {
                _shardingPageManager.Current.RouteQueryResults.Add(new RouteQueryResult<long>(routeQueryResult.DataSourceName, routeQueryResult.TableRouteResult, routeQueryResult.QueryResult));
                r += routeQueryResult.QueryResult;
            }

            return new RouteQueryResult<long>(null,null,r,true);
        }
        return new RouteQueryResult<long>(null,null,parallelResults.Sum(o => o.QueryResult),true);
    }

    public void InMemoryMerge(List<RouteQueryResult<long>> beforeInMemoryResults, List<RouteQueryResult<long>> parallelResults)
    {
        beforeInMemoryResults.AddRange(parallelResults);
    }
}
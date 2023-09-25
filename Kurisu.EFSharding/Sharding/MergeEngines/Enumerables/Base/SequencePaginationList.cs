using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables.Base;

internal class SequencePaginationList
{
    private readonly IEnumerable<RouteQueryResult<long>> _routeQueryResults;
    private long? _skip;
    private long? _take;

    public SequencePaginationList(IEnumerable<RouteQueryResult<long>> routeQueryResults)
    {
        _routeQueryResults = routeQueryResults;
    }
    public SequencePaginationList Skip(long? skip)
    {
        if (skip > int.MaxValue)
            throw new ShardingCoreException($"not support skip more than {int.MaxValue}");
        _skip = skip;
        return this;
    }
    public SequencePaginationList Take(long? take)
    {
        if (take > int.MaxValue)
            throw new ShardingCoreException($"not support take more than {int.MaxValue}");
        _take = take;
        return this;
    }

    public ICollection<SequenceResult> ToList()
    {
        ICollection<SequenceResult> routeResults = new LinkedList<SequenceResult>();

        var currentSkip = _skip.GetValueOrDefault();
        var currentTake = _take;
        bool stopSkip = false;
        bool needBreak = false;
        foreach (var routeQueryResult in _routeQueryResults)
        {
            if (!stopSkip)
            {
                if (routeQueryResult.QueryResult > currentSkip)
                {
                    stopSkip = true;
                }
                else
                {
                    currentSkip = currentSkip - routeQueryResult.QueryResult;
                    continue;
                }
            }

            var currentRealSkip = currentSkip;
            var currentRealTake = routeQueryResult.QueryResult-currentSkip;
            if (currentSkip != 0L)
                currentSkip = 0;
            if (currentTake.HasValue)
            {
                if (currentTake.Value <= currentRealTake)
                {
                    currentRealTake = currentTake.Value;
                    needBreak = true;
                }
                else
                {
                    currentTake = currentTake.Value - currentRealTake;
                }
            }
            var sequenceResult = new SequenceResult(currentRealSkip, currentRealTake, routeQueryResult);
            routeResults.Add(sequenceResult);

            if (needBreak)
                break;

        }

        return routeResults;
    }
}
public class SequenceResult
{
    public SequenceResult(long skip, long take,RouteQueryResult<long> routeQueryResult)
    {
        Skip = (int)skip;
        Take = (int)take;
        DSName = routeQueryResult.DataSourceName;
        TableRouteResult = routeQueryResult.TableRouteResult;
    }

    public int Skip { get; }
    public int Take { get; }

    public string DSName { get; }
    public TableRouteResult TableRouteResult { get; }
}
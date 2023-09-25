using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

namespace Kurisu.EFSharding.Core.UnionAllMergeShardingProviders;

public class UnionAllMergeContext
{
    public UnionAllMergeContext(IEnumerable<TableRouteResult> tableRoutesResults)
    {
        TableRoutesResults = tableRoutesResults;
    }

    public IEnumerable<TableRouteResult> TableRoutesResults { get; }
}
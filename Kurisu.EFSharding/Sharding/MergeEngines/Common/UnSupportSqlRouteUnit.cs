using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Common;

public class UnSupportSqlRouteUnit : ISqlRouteUnit
{
    public UnSupportSqlRouteUnit(string dataSourceName, List<TableRouteResult> tableRouteResults)
    {
        DatasourceName = dataSourceName;
        var routeResults = tableRouteResults;
        TableRouteResults = routeResults;
        TableRouteResult = new TableRouteResult(new List<TableRouteUnit>(0));
    }

    public string DatasourceName { get; }
    public TableRouteResult TableRouteResult { get; }
    public List<TableRouteResult> TableRouteResults { get; }
}
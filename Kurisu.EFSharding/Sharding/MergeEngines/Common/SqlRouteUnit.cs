using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Common;

public class SqlRouteUnit : ISqlRouteUnit
{
    public SqlRouteUnit(string dataSourceName, TableRouteResult tableRouteResult)
    {
        DatasourceName = dataSourceName;
        TableRouteResult = tableRouteResult;
    }

    public string DatasourceName { get; }
    public TableRouteResult TableRouteResult { get; }

    public override string ToString()
    {
        return $"{nameof(DatasourceName)}:{DatasourceName},{nameof(TableRouteResult)}:{TableRouteResult}";
    }
}
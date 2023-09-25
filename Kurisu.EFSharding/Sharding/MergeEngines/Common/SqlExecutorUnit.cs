using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Common;

internal class SqlExecutorUnit
{
    public SqlExecutorUnit(ISqlRouteUnit routeUnit)
    {
        RouteUnit = routeUnit;
    }

    public ISqlRouteUnit RouteUnit { get; }
}
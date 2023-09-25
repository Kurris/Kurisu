using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

namespace Kurisu.EFSharding.Core.VirtualRoutes;

public class ShardingRouteResult
{
    public IReadOnlyList<ISqlRouteUnit> RouteUnits { get; }
    public bool IsCrossDataSource { get; }
    public bool IsCrossTable { get; }
    public bool ExistCrossTableTails { get; }
    public bool IsEmpty { get; }

    public ShardingRouteResult(IReadOnlyList<ISqlRouteUnit> routeUnits, bool isEmpty, bool isCrossDataSource, bool isCrossTable, bool existCrossTableTails)
    {
        RouteUnits = routeUnits;
        IsEmpty = isEmpty;
        IsCrossDataSource = isCrossDataSource;
        IsCrossTable = isCrossTable;
        ExistCrossTableTails = existCrossTableTails;
    }

    public override string ToString()
    {
        return string.Join(",", RouteUnits.Select(o => o.ToString()));
    }
}
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

public interface ISqlRouteUnit
{
    string DatasourceName { get; }

    TableRouteResult TableRouteResult { get; }
}
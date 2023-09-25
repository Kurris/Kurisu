namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

public interface ITableRouteRuleEngine
{
    ShardingRouteResult Route(TableRouteRuleContext tableRouteRuleContext);
}
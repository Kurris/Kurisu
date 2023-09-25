using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;

public interface IRouteTailFactory
{
    IRouteTail Create(string tail);

    IRouteTail Create(string tail, bool cache);

    IRouteTail Create(TableRouteResult tableRouteResult);

    IRouteTail Create(TableRouteResult tableRouteResult, bool cache);
}
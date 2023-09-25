using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

namespace Kurisu.EFSharding.Core.VirtualRoutes.datasourceRoutes.RouteRuleEngine;

public interface IDatasourceRouteRuleEngine
{
    DatasourceRouteResult Route(DatasourceRouteRuleContext routeRuleContext);
}
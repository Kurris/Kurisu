using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

public class TableRouteRuleContext
{
    public TableRouteRuleContext(DatasourceRouteResult datasourceRouteResult, IQueryable queryable, Dictionary<Type, IQueryable> queryEntities)
    {
        DatasourceRouteResult = datasourceRouteResult;
        Queryable = queryable;
        QueryEntities = queryEntities;
    }

    public DatasourceRouteResult DatasourceRouteResult { get; }
    public IQueryable Queryable { get; }
    public Dictionary<Type, IQueryable> QueryEntities { get; }
}
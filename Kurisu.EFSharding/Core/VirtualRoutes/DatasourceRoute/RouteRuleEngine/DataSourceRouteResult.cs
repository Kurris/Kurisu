namespace Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

public class DatasourceRouteResult
{
    public DatasourceRouteResult(ISet<string> intersectDataSources)
    {
        IntersectDataSources = intersectDataSources;
    }

    public DatasourceRouteResult(string datasource) : this(new HashSet<string> {datasource})
    {
    }

    /// <summary>
    /// 交集
    /// </summary>
    public ISet<string> IntersectDataSources { get; }

    public override string ToString()
    {
        return $"data source route result:{string.Join(",", IntersectDataSources)}";
    }
}
using System.Collections.Generic;

namespace Kurisu.DataAccess.Sharding.VirtualRoute;

public class DatasourceRouteResult
{
    public DatasourceRouteResult(ISet<string> intersectDataSources)
    {
        IntersectDataSources = intersectDataSources;
    }

    public DatasourceRouteResult(string dataSource) : this(new HashSet<string>() {dataSource})
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

using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

namespace Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

/// <summary>
/// 路由断言
/// </summary>
public interface ITableRouteAssert
{
    void Assert(DatasourceRouteResult datasourceRouteResult, List<string> tails, List<TableRouteUnit> shardingRouteUnits);
}

public interface ITableRouteAssert<T> : ITableRouteAssert where T : class
{
}
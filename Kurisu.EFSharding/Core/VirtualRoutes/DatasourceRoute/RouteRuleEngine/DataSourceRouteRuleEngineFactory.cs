using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.datasourceRoutes.RouteRuleEngine;

/// <summary>
/// 分库路由引擎工程
/// </summary>
public class DatasourceRouteRuleEngineFactory : IDatasourceRouteRuleEngineFactory
{
    private readonly IDatasourceRouteRuleEngine _datasourceRouteRuleEngine;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="datasourceRouteRuleEngine"></param>
    public DatasourceRouteRuleEngineFactory(IDatasourceRouteRuleEngine datasourceRouteRuleEngine)
    {
        _datasourceRouteRuleEngine = datasourceRouteRuleEngine;
    }

    /// <summary>
    /// 通过表达式创建分库路由上下文
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="shardingDbContext"></param>
    /// <param name="queryEntities"></param>
    /// <returns></returns>
    private DatasourceRouteRuleContext CreateContext(IQueryable queryable, IShardingDbContext shardingDbContext, Dictionary<Type, IQueryable> queryEntities)
    {
        return new DatasourceRouteRuleContext(queryable, shardingDbContext, queryEntities);
    }

    /// <summary>
    /// 路由到具体的物理数据源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="shardingDbContext"></param>
    /// <param name="queryEntities"></param>
    /// <returns></returns>
    public DatasourceRouteResult Route(IQueryable queryable, IShardingDbContext shardingDbContext, Dictionary<Type, IQueryable> queryEntities)
    {
        var ruleContext = CreateContext(queryable, shardingDbContext, queryEntities);
        return _datasourceRouteRuleEngine.Route(ruleContext);
    }

}
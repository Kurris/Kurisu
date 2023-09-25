using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;

/// <summary>
/// 分库路由上下文
/// </summary>
/// <typeparam name="T"></typeparam>
public class DatasourceRouteRuleContext
{
    public DatasourceRouteRuleContext(IQueryable queryable, IShardingDbContext shardingDbContext, Dictionary<Type, IQueryable> queryEntities)
    {
        Queryable = queryable;
        ShardingDbContext = shardingDbContext;
        QueryEntities = queryEntities;
    }

    public Dictionary<Type, IQueryable> QueryEntities { get; }

    /// <summary>
    /// 查询条件
    /// </summary>
    public IQueryable Queryable { get; }

    public IShardingDbContext ShardingDbContext { get; }
}
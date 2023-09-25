using System.Linq.Expressions;
using Kurisu.EFSharding.Core.QueryRouteManagers;
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Sharding.Parsers.Abstractions;

public interface IPrepareParseResult
{
    /// <summary>
    /// 获取当前分片上下文
    /// </summary>
    /// <returns></returns>
    IShardingDbContext GetShardingDbContext();

    /// <summary>
    /// 获取原始的查询表达式
    /// </summary>
    /// <returns></returns>
    Expression GetNativeQueryExpression();

    /// <summary>
    /// 是否使用union all 聚合
    /// </summary>
    /// <returns></returns>
    bool UseUnionAllMerge();

    /// <summary>
    /// 当前查询的连接数限制
    /// </summary>
    /// <returns></returns>
    int? GetMaxQueryConnectionsLimit();

    /// <summary>
    /// 在启用读写分离后如果设置了readonly那么就走readonly否则为null
    /// </summary>
    /// <returns></returns>
    bool? ReadOnly();

    /// <summary>
    /// 自定义路由
    /// </summary>
    /// <returns></returns>
    Action<ShardingRouteContext> GetAsRoute();

    bool? IsSequence();
    bool? SameWithShardingComparer();
    Dictionary<Type /* 查询对象类型 */, IQueryable /* 查询对象对应的表达式 */> GetQueryEntities();
    bool? IsNotracking();
    bool IsIgnoreFilter();
}
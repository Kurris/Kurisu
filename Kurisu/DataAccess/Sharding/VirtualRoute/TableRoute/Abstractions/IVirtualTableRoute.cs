using System.Collections.Generic;
using System.Linq;
using Kurisu.DataAccess.Sharding.Metadata;
using Kurisu.DataAccess.Sharding.Metadata.Abstractions;

namespace Kurisu.DataAccess.Sharding.VirtualRoute.TableRoute.Abstractions;

/// <summary>
/// 虚拟路由:表
/// </summary>
public interface IVirtualTableRoute<TEntity> : IEntityMetadataConfiguration<TEntity> where TEntity : class, new()
{
    EntityMetadata Metadata { get; }


    /// <summary>
    /// 根据查询条件路由返回物理表
    /// </summary>
    /// <param name="datasourceRouteResult"></param>
    /// <param name="queryable"></param>
    /// <param name="isQuery"></param>
    /// <returns></returns>
    List<TableRouteUnit> RouteWithPredicate(DatasourceRouteResult datasourceRouteResult, IQueryable queryable, bool isQuery);

    /// <summary>
    /// 根据值进行路由
    /// </summary>
    /// <param name="datasourceRouteResult"></param>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    TableRouteUnit RouteWithValue(DatasourceRouteResult datasourceRouteResult, object shardingKey);

    /// <summary>
    /// 获取
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetTails();


    /// <summary>
    /// 是否启用表达式分片配置
    /// </summary>
    bool EnableEntityQuery { get; }
}
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Sharding.EntityQueryConfigurations;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;

public interface IVirtualTableRoute
{
    /// <summary>
    /// 元数据
    /// </summary>
    BaseShardingMetadata Metadata { get; }

    string ToTail(object value);

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


    List<string> GetTails();

    /// <summary>
    /// 分页配置
    /// </summary>
    PaginationMetadata PaginationMetadata { get; }

    /// <summary>
    /// 是否启用智能分页
    /// </summary>
    bool EnablePagination { get; } // PaginationMetadata != null;

    /// <summary>
    /// 查询配置
    /// </summary>
    EntityQueryMetadata EntityQueryMetadata { get; }

    bool EnableEntityQuery { get; }
}

public interface IVirtualTableRoute<TEntity> : IVirtualTableRoute, IMetadataConfiguration<TEntity>
    where TEntity : class, new()
{
    IPaginationConfiguration<TEntity> CreatePaginationConfiguration();

    IEntityQueryConfiguration<TEntity> CreateEntityQueryConfiguration();
}
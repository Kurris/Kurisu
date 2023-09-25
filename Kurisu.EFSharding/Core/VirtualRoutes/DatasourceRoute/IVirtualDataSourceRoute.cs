using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Sharding.EntityQueryConfigurations;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;

public interface IVirtualDatasourceRoute
{
    BaseShardingMetadata Metadata { get; }

    /// <summary>
    /// 分页配置
    /// </summary>
    PaginationMetadata PaginationMetadata { get; }

    /// <summary>
    /// 是否启用分页配置
    /// </summary>
    bool EnablePagination { get; }

    string ShardingKeyToDataSourceName(object shardingKeyValue);

    /// <summary>
    /// 根据查询条件路由返回物理数据源
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="isQuery"></param>
    /// <returns>data source name</returns>
    List<string> RouteWithPredicate(IQueryable queryable, bool isQuery);

    /// <summary>
    /// 根据值进行路由
    /// </summary>
    /// <param name="shardingKeyValue"></param>
    /// <returns>data source name</returns>
    string RouteWithValue(object shardingKeyValue);

    List<string> GetAllDatasourceNames();

    /// <summary>
    /// 添加数据源
    /// </summary>
    /// <param name="dataSourceName"></param>
    /// <returns></returns>
    bool AddDatasourceName(string dataSourceName);
}

public interface IVirtualDatasourceRoute<TEntity> : IVirtualDatasourceRoute, IMetadataConfiguration<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// 返回null就是表示不开启分页配置
    /// </summary>
    /// <returns></returns>
    IPaginationConfiguration<TEntity> CreatePaginationConfiguration();

    ///// <summary>
    ///// 配置查询
    ///// </summary>
    ///// <returns></returns>
    IEntityQueryConfiguration<TEntity> CreateEntityQueryConfiguration();
}
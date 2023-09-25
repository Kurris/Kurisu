using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Core.Metadata.Builder;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Sharding.EntityQueryConfigurations;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

public abstract class BaseVirtualTable<TEntity> : IVirtualTableRoute<TEntity>, IMetadataInitializer
    where TEntity : class, new()
{
    protected IShardingProvider ShardingProvider { get; }

    protected BaseVirtualTable(IShardingProvider shardingProvider)
    {
        ShardingProvider = shardingProvider;
    }

    public PaginationMetadata PaginationMetadata { get; private set; }
    public bool EnablePagination => PaginationMetadata != null;
    public EntityQueryMetadata EntityQueryMetadata { get; private set; }
    public bool EnableEntityQuery => EntityQueryMetadata != null;

    public BaseShardingMetadata Metadata { get; private set; }

    public virtual void Initialize(BaseShardingMetadata entityMetadata)
    {
        Metadata = entityMetadata;
        // RouteConfigOptions =shardingProvider.GetService<IShardingRouteConfigOptions>();
        var paginationConfiguration = CreatePaginationConfiguration();
        if (paginationConfiguration != null)
        {
            PaginationMetadata = new PaginationMetadata();
            var paginationBuilder = new PaginationBuilder<TEntity>(PaginationMetadata);
            paginationConfiguration.Configure(paginationBuilder);
        }

        var entityQueryConfiguration = CreateEntityQueryConfiguration();
        if (entityQueryConfiguration != null)
        {
            EntityQueryMetadata = new EntityQueryMetadata();
            var entityQueryBuilder = new EntityQueryBuilder<TEntity>(EntityQueryMetadata);
            entityQueryConfiguration.Configure(entityQueryBuilder);
        }
    }


    public abstract string ToTail(object value);

    /// <summary>
    /// 根据表达式路由
    /// </summary>
    /// <param name="datasourceRouteResult"></param>
    /// <param name="queryable"></param>
    /// <param name="isQuery"></param>
    /// <returns></returns>
    public abstract List<TableRouteUnit> RouteWithPredicate(DatasourceRouteResult datasourceRouteResult, IQueryable queryable, bool isQuery);

    /// <summary>
    /// 根据值路由
    /// </summary>
    /// <param name="datasourceRouteResult"></param>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    public abstract TableRouteUnit RouteWithValue(DatasourceRouteResult datasourceRouteResult, object shardingKey);

    /// <summary>
    /// 返回数据库现有的尾巴
    /// </summary>
    /// <returns></returns>
    public abstract List<string> GetTails();

    /// <summary>
    /// 创建分页配置
    /// </summary>
    /// <returns></returns>
    public virtual IPaginationConfiguration<TEntity> CreatePaginationConfiguration()
    {
        return null;
    }

    /// <summary>
    /// 创建熟悉怒查询配置
    /// </summary>
    /// <returns></returns>
    public virtual IEntityQueryConfiguration<TEntity> CreateEntityQueryConfiguration()
    {
        return null;
    }

    public abstract void Configure(IShardingMetadataBuilder<TEntity> builder);
}
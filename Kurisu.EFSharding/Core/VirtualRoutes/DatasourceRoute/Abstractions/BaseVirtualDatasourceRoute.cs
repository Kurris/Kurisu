using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Core.Metadata.Builder;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Sharding.EntityQueryConfigurations;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute.Abstractions;

public abstract class BaseVirtualDatasourceRoute<TEntity, TKey> : IVirtualDatasourceRoute<TEntity>, IMetadataInitializer
    where TEntity : class, new()
{
    protected IShardingProvider ShardingProvider { get; }
    public BaseShardingMetadata Metadata { get; private set; }

    public new PaginationMetadata PaginationMetadata { get; protected set; }
    public bool EnablePagination => PaginationMetadata != null;

    protected BaseVirtualDatasourceRoute(IShardingProvider shardingProvider)
    {
        ShardingProvider = shardingProvider;
    }

    public void Initialize(BaseShardingMetadata entityMetadata)
    {
        Metadata = entityMetadata;

        var paginationConfiguration = CreatePaginationConfiguration();
        if (paginationConfiguration != null)
        {
            PaginationMetadata = new PaginationMetadata();
            var paginationBuilder = new PaginationBuilder<TEntity>(PaginationMetadata);
            paginationConfiguration.Configure(paginationBuilder);
        }
    }

    public virtual IPaginationConfiguration<TEntity> CreatePaginationConfiguration()
    {
        return null;
    }

    public IEntityQueryConfiguration<TEntity> CreateEntityQueryConfiguration()
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// 分库字段如何转成对应的数据源名称 how  convert sharding data source key to data source name
    /// </summary>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    public abstract string ShardingKeyToDataSourceName(object shardingKey);

    /// <summary>
    /// 根据表达式返回对应的数据源名称 find data source names with queryable
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="isQuery"></param>
    /// <returns></returns>
    public abstract List<string> RouteWithPredicate(IQueryable queryable, bool isQuery);

    /// <summary>
    /// 值如何转成对应的数据源
    /// </summary>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    public abstract string RouteWithValue(object shardingKey);

    public abstract List<string> GetAllDatasourceNames();
    public abstract bool AddDatasourceName(string dataSourceName);

    public abstract void Configure(IShardingMetadataBuilder<TEntity> builder);
}
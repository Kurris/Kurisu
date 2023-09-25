using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.Metadata.Model;

namespace Kurisu.EFSharding.Extensions;

public static class EntityMetadataManagerExtension
{
    public static List<BaseShardingMetadata> TryGet<TEntity>(this IMetadataManager entityMetadataManager)
        where TEntity : class
    {
        return entityMetadataManager.TryGet(typeof(TEntity));
    }

    public static bool IsShardingTable<TEntity>(this IMetadataManager entityMetadataManager) where TEntity : class
    {
        return entityMetadataManager.IsShardingTable(typeof(TEntity));
    }

    public static bool IsShardingDatasource<TEntity>(this IMetadataManager entityMetadataManager) where TEntity : class
    {
        return entityMetadataManager.IsShardingDatasource(typeof(TEntity));
    }
}
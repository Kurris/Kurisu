using Kurisu.EFSharding.Core.Metadata.Model;

namespace Kurisu.EFSharding.Extensions;

public static class EntityMetadataExtension
{
    public static bool ShardingDatasourceFieldIsKey(this BaseShardingMetadata metadata)
    {
        if (!metadata.IsDatasourceMetadata && metadata.IsSingleKey)
            return false;
        return metadata.Property.Name == metadata.PrimaryKeys.FirstOrDefault()?.Name;
    }

    public static bool ShardingTableFieldIsKey(this BaseShardingMetadata metadata)
    {
        if (!metadata.IsTableMetadata && metadata.IsSingleKey)
            return false;
        return metadata.Property.Name == metadata.PrimaryKeys.FirstOrDefault()?.Name;
    }

    public static bool IsMainShardingTableKey(this BaseShardingMetadata metadata, string shardingPropertyName)
    {
        if (metadata.Property.Name == shardingPropertyName)
            return true;
        return false;
    }

    public static bool IsMainShardingDatasourceKey(this BaseShardingMetadata metadata, string shardingPropertyName)
    {
        if (metadata.Property.Name == shardingPropertyName)
            return true;
        return false;
    }
}
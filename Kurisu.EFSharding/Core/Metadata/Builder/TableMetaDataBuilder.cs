using Kurisu.EFSharding.Core.Metadata.Model;

namespace Kurisu.EFSharding.Core.Metadata.Builder;

/// <summary>
/// 分表对象元数据builder
/// </summary>
/// <typeparam name="TEntity"></typeparam>
internal class TableMetadataBuilder<TEntity> : BaseShardingMetadataBuilder<TEntity>
    where TEntity : class, new()
{
    private TableMetadataBuilder(BaseShardingMetadata metadata) : base(metadata)
    {
    }


    public static IShardingMetadataBuilder<TEntity> Create(BaseShardingMetadata metadata)
    {
        return new TableMetadataBuilder<TEntity>(metadata);
    }
}
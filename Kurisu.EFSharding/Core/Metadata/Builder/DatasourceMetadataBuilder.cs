using Kurisu.EFSharding.Core.Metadata.Model;

namespace Kurisu.EFSharding.Core.Metadata.Builder;

internal class DatasourceMetadataBuilder<TEntity> : BaseShardingMetadataBuilder<TEntity>
    where TEntity : class, new()
{
    private DatasourceMetadataBuilder(BaseShardingMetadata metadata) : base(metadata)
    {
    }


    public static IShardingMetadataBuilder<TEntity> Create(BaseShardingMetadata metadata)
    {
        return new DatasourceMetadataBuilder<TEntity>(metadata);
    }
}
namespace Kurisu.EFSharding.Core.Metadata.Model;

internal class DatasourceShardingMetadata : BaseShardingMetadata
{
    private DatasourceShardingMetadata(Type entityType) : base(entityType)
    {
    }

    public override bool IsDatasourceMetadata => true;
    public override bool IsTableMetadata => !IsDatasourceMetadata;


    internal static BaseShardingMetadata Create(Type entityType)
    {
        return new DatasourceShardingMetadata(entityType);
    }
}
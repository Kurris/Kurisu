namespace Kurisu.EFSharding.Core.Metadata.Model;

internal class TableShardingMetadata : BaseShardingMetadata
{
    private TableShardingMetadata(Type entityType) : base(entityType)
    {
    }

    public override bool IsDatasourceMetadata => !IsTableMetadata;
    public override bool IsTableMetadata => true;


    internal static BaseShardingMetadata Create(Type entityType)
    {
        return new TableShardingMetadata(entityType);
    }
}
using Kurisu.EFSharding.Core.ShardingMigrations.Abstractions;

namespace Kurisu.EFSharding.Core.ShardingMigrations;

public class ShardingMigrationAccessor:IShardingMigrationAccessor
{
    private static AsyncLocal<ShardingMigrationContext> _shardingMigrationContext = new AsyncLocal<ShardingMigrationContext>();
    public ShardingMigrationContext ShardingMigrationContext
    {
        get => _shardingMigrationContext.Value;
        set => _shardingMigrationContext.Value = value;
    }

}
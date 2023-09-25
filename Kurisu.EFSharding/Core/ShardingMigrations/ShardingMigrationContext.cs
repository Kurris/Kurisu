namespace Kurisu.EFSharding.Core.ShardingMigrations;

public class ShardingMigrationContext
{
    /// <summary>
    /// 当前的数据源名称
    /// </summary>
    public string CurrentDatasourceName { get; set; }

    public static ShardingMigrationContext Create()
    {
        return new ShardingMigrationContext();
    }
}
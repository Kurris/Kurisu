namespace Kurisu.EFSharding.TableCreator;

public interface IShardingTableCreator
{
    /// <summary>
    /// 创建表
    /// </summary>
    /// <param name="dataSourceName"></param>
    /// <param name="entityType"></param>
    /// <param name="tail"></param>
    Task CreateTableAsync(string dataSourceName, Type entityType, string tail);
}
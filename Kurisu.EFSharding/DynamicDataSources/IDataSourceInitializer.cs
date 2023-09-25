namespace Kurisu.EFSharding.Dynamicdatasources;

public interface IDatasourceInitializer
{
    /// <summary>
    /// 动态初始化数据源仅创建
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <param name="createDatabase"></param>
    /// <param name="createTable"></param>
    Task InitConfigureAsync(string datasourceName, bool createDatabase, bool createTable);
}
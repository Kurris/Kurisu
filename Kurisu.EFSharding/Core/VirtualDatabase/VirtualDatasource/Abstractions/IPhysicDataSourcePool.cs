namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;

/// <summary>
/// 数据源池
/// </summary>
public interface IPhysicDatasourcePool
{
    /// <summary>
    /// 添加一个物理数据源
    /// </summary>
    /// <param name="physicDataSource"></param>
    /// <returns></returns>
    bool TryAdd(DatasourceUnit physicDataSource);

    /// <summary>
    /// 尝试获取一个物理数据源没有返回null
    /// </summary>
    /// <param name="dataSourceName"></param>
    /// <returns></returns>
    DatasourceUnit TryGet(string dataSourceName);

    /// <summary>
    /// 获取所有的数据源名称
    /// </summary>
    /// <returns></returns>
    List<string> GetAllDatasourceNames();

    IDictionary<string, string> GetDatasource();
}
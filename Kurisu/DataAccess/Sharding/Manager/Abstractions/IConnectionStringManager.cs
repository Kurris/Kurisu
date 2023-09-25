namespace Kurisu.DataAccess.Sharding.Manager.Abstractions;

public interface IConnectionStringManager
{
    /// <summary>
    /// 获取链接
    /// </summary>
    /// <param name="name">数据源名称</param>
    /// <returns></returns>
    string GetConnectionString(string name);
}
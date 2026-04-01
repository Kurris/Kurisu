namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 数据源管理器
/// </summary>
public interface IDatasourceManager<TClient> : IDatasourceManager
{
    /// <summary>
    /// 获取当前Db客户端
    /// </summary>
    /// <returns></returns>
    TClient GetCurrentClient();
}

public interface IDatasourceManager : ITransactionManager
{
    TClient GetCurrentClient<TClient>();

    /// <summary>
    /// 创建新的数据源作用域
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IDisposable CreateScope(string name);
}

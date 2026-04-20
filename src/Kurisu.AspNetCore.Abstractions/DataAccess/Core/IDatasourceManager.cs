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

/// <summary>
/// 数据源管理器
/// </summary>
public interface IDatasourceManager : ITransactionManager
{
    /// <summary>
    /// 获取当前Db客户端
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    /// <returns></returns>
    TClient GetCurrentClient<TClient>();
}

using System.Data;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services;

/// <summary>
/// 事务隔离级别
/// </summary>
public interface IIsolationLevelService
{
    /// <summary>
    /// 设置隔离级别
    /// </summary>
    /// <param name="isolationLevel"></param>
    void Set(IsolationLevel isolationLevel);

    /// <summary>
    /// 获取隔离级别
    /// </summary>
    /// <returns></returns>
    IsolationLevel Get();
}
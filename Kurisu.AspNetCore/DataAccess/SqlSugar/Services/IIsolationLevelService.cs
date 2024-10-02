using System.Data;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services;

/// <summary>
/// 事务隔离级别
/// </summary>
public interface IIsolationLevelService 
{
    void Set(IsolationLevel isolationLevel);

    IsolationLevel Get();
}
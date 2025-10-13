using System.Data;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 数据库上下文
/// </summary>
[SkipScan]
public interface IDbContext
{
    /// <summary>
    /// 切换数据库
    /// </summary>
    /// <param name="dbId"></param>
    /// <returns></returns>
    IDbContext ChangeDb(string dbId);

    Task<long> InsertReturnIdentityAsync<T>(T obj) where T : class, new();
    
    long InsertReturnIdentity<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T obj) where T : class, new();
    int Insert<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T[] obj) where T : class, new();
    int Insert<T>(T[] obj) where T : class, new();

    Task<int> InsertAsync<T>(List<T> obj) where T : class, new();
    int Insert<T>(List<T> obj) where T : class, new();

    Task<int> DeleteAsync<T>(T obj, bool isReally = false) where T : class, new();
    int Delete<T>(T obj, bool isReally = false) where T : class, new();

    Task<int> DeleteAsync<T>(T[] obj, bool isReally = false) where T : class, new();
    int Delete<T>(T[] obj, bool isReally = false) where T : class, new();

    Task<int> DeleteAsync<T>(List<T> obj, bool isReally = false) where T : class, new();
    int Delete<T>(List<T> obj, bool isReally = false) where T : class, new();

    Task<int> UpdateAsync<T>(T obj) where T : class, new();

    Task<int> UpdateAsync<T>(T[] obj) where T : class, new();

    Task<int> UpdateAsync<T>(List<T> obj) where T : class, new();

    int Update<T>(T obj) where T : class, new();

    int Update<T>(T[] obj) where T : class, new();

    int Update<T>(List<T> obj) where T : class, new();

    Task UseTransactionAsync(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    void UseTransaction(Action action, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    Task IgnoreTenantAsync(Func<Task> func);
    void IgnoreTenant(Action func);

    Task IgnoreSoftDeletedAsync(Func<Task> func);
    void IgnoreSoftDeleted(Action func);
}
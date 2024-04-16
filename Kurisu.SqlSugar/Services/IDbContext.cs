using System.Data;
using System.Linq.Expressions;
using Kurisu.Core.DataAccess.Entity;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.SqlSugar.Services;

/// <summary>
/// 数据库上下文
/// </summary>
[SkipScan]
public interface IDbContext
{
    /// <summary>
    /// 使用原生sugar client
    /// </summary>
    /// <remarks>
    /// 将会脱离IDbContext的控制
    /// </remarks>
    public ISqlSugarClient Client { get; }

    /// <summary>
    /// 查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ISugarQueryable<T> Queryable<T>();

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


    Task<int> DeleteAsync<T>(T obj) where T : class, ISoftDeleted, new();
    int Delete<T>(T obj) where T : class, ISoftDeleted, new();

    Task<int> DeleteAsync<T>(T[] obj) where T : class, ISoftDeleted, new();
    int Delete<T>(T[] obj) where T : class, ISoftDeleted, new();

    Task<int> DeleteAsync<T>(List<T> obj) where T : class, ISoftDeleted, new();
    int Delete<T>(List<T> obj) where T : class, ISoftDeleted, new();

    IDeleteable<T> Deleteable<T>() where T : class, new();

    Task<int> DeleteReallyAsync<T>(T obj) where T : class, new();

    Task<int> DeleteReallyAsync<T>(Expression<Func<T, bool>> expression) where T : class, new();

    Task<int> DeleteReallyAsync<T>(List<T> obj) where T : class, new();


    int DeleteReally<T>(T obj) where T : class, new();

    int DeleteReally<T>(Expression<Func<T, bool>> expression) where T : class, new();

    int DeleteReally<T>(List<T> obj) where T : class, new();


    Task<int> UpdateAsync<T>(T obj) where T : class, new();

    Task<int> UpdateAsync<T>(T[] obj) where T : class, new();

    Task<int> UpdateAsync<T>(List<T> obj) where T : class, new();

    int Update<T>(T obj) where T : class, new();

    int Update<T>(T[] obj) where T : class, new();

    int Update<T>(List<T> obj) where T : class, new();

    IUpdateable<T> Updateable<T>() where T : class, new();

    Task UseTransactionAsync(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    void UseTransaction(Action action, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    Task IgnoreAsync<T>(Func<Task> func);

    void Ignore<T>(Action action);
}
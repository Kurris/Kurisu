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

    Task<int> InsertAsync<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T[] obj) where T : class, new();

    Task<int> InsertAsync<T>(List<T> obj) where T : class, new();


    Task<int> DeleteAsync<T>(T obj) where T : class, ISoftDeleted, new();

    Task<int> DeleteAsync<T>(T[] obj) where T : class, ISoftDeleted, new();

    Task<int> DeleteAsync<T>(List<T> obj) where T : class, ISoftDeleted, new();


    Task<int> DeleteReallyAsync<T>(T obj) where T : class, new();

    Task<int> DeleteReallyAsync<T>(Expression<Func<T, bool>> expression) where T : class, new();

    Task<int> DeleteReallyAsync<T>(List<T> obj) where T : class, new();


    Task<int> UpdateAsync<T>(T obj) where T : class, new();

    Task<int> UpdateAsync<T>(T[] obj) where T : class, new();

    Task<int> UpdateAsync<T>(List<T> obj) where T : class, new();

    IUpdateable<T> Updateable<T>() where T : class, new();

    Task<DbResult<T>> UseTransactionAsync<T>(Func<Task<T>> func, Action<Exception> callback = null);

    DbResult<T> UseTransaction<T>(Func<T> func, Action<Exception> callback = null);
}
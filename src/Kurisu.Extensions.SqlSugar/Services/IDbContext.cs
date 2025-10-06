using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Extensions.SqlSugar.Services;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services;

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

    IQueryableSetting GetQueryableSetting();

    Task<long> InsertReturnIdentityAsync<T>(T obj) where T : class, new();
    long InsertReturnIdentity<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T obj) where T : class, new();
    int Insert<T>(T obj) where T : class, new();

    Task<int> InsertAsync<T>(T[] obj) where T : class, new();
    int Insert<T>(T[] obj) where T : class, new();

    Task<int> InsertAsync<T>(List<T> obj) where T : class, new();
    int Insert<T>(List<T> obj) where T : class, new();

    Task<int> SaveAsync<T>(T obj) where T : SugarBaseEntity, new();
    int Save<T>(T obj) where T : SugarBaseEntity, new();

    Task<int> DeleteAsync<T>(T obj, bool isReally = false) where T : class, new();
    int Delete<T>(T obj, bool isReally = false) where T : class, new();

    Task<int> DeleteAsync<T>(T[] obj, bool isReally = false) where T : class, new();
    int Delete<T>(T[] obj, bool isReally = false) where T : class, new();

    Task<int> DeleteAsync<T>(List<T> obj, bool isReally = false) where T : class, new();
    int Delete<T>(List<T> obj, bool isReally = false) where T : class, new();

    IDeleteable<T> Deleteable<T>() where T : class, new();


    Task<int> UpdateAsync<T>(T obj) where T : class, new();

    Task<int> UpdateAsync<T>(T[] obj) where T : class, new();

    Task<int> UpdateAsync<T>(List<T> obj) where T : class, new();

    int Update<T>(T obj) where T : class, new();

    int Update<T>(T[] obj) where T : class, new();

    int Update<T>(List<T> obj) where T : class, new();

    IUpdateable<T> Updateable<T>() where T : class, new();

    Task UseTransactionAsync(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    void UseTransaction(Action action, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);

    Task IgnoreTenantAsync(Func<Task> func);
    void IgnoreTenant(Action func);

    Task IgnoreSoftDeletedAsync(Func<Task> func);
    void IgnoreSoftDeleted(Action func);
}
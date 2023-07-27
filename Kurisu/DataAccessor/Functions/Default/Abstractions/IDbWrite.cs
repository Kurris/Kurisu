using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Functions.Default.Abstractions;

/// <summary>
/// 写入接口
/// </summary>
public interface IDbWrite : IDbRead
{
    /// <summary>
    /// 保存更新
    /// </summary>
    /// <returns></returns>
    Task<int> SaveChangesAsync();

    #region sql

    /// <summary>
    /// 执行SQL
    /// </summary>
    /// <param name="sql">sql字符串</param>
    /// <param name="args">参数</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    /// <remarks>参数化:Add into blog values({0},{1}) </remarks>
    Task<int> RunSqlAsync(string sql, params object[] args);

    /// <summary>
    /// 执行SQL(插值)
    /// </summary>
    /// <param name="sql">内插sql字符串</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    /// <remarks>参数化:$"Add into blog values({value1},{value2})" </remarks>
    Task<int> RunSqlAsync(FormattableString sql);

    #endregion

    #region save

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    ValueTask SaveAsync(object entity);

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask SaveAsync<T>(T entity) where T : class, new();

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    ValueTask SaveRangeAsync(IEnumerable<object> entities);

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask SaveRangeAsync<T>(IEnumerable<T> entities) where T : class, new();

    #endregion

    #region add

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="entity">实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    ValueTask AddAsync(object entity);

    /// <summary>
    /// 添加
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    ValueTask AddAsync<T>(T entity) where T : class, new();

    /// <summary>
    /// 添加
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">一组实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class, new();


    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task AddRangeAsync(IEnumerable<object> entities);

    #endregion

    #region update

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="entity">实例</param>
    /// <param name="all">全部更新</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task UpdateAsync(object entity, bool all = false);

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实例</param>
    /// <param name="all">全部更新</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task UpdateAsync<T>(T entity, bool all = false) where T : class, new();

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">一组实例</param>
    /// <param name="all">全部更新</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool all = false) where T : class, new();

    Task UpdateRangeAsync(IEnumerable<object> entities, bool all = false);

    #endregion

    #region delete

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new();

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task DeleteAsync(object entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task DeleteAsync<T>(T entity) where T : class, new();

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">一组实例</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new();


    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task DeleteRangeAsync(IEnumerable<object> entities);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="ids">主键</param>
    /// <returns>返回受影响行<see cref="int"/></returns>
    Task DeleteByIdsAsync<T>(params object[] ids) where T : class, new();

    #endregion

    #region transaction

    /// <summary>
    /// 使用事务
    /// </summary>
    /// <param name="func"></param>
    /// <returns></returns>
    Task<int> UseTransactionAsync(Func<Task> func);

    /// <summary>
    /// 开启事务
    /// </summary>
    /// <returns></returns>
    Task<IDbContextTransaction> BeginTransactionAsync();

    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;

/// <summary>
/// 从库接口,读操作
/// </summary>
public interface IAppSlaveDb : IBaseDbService
{
    /// <summary>
    /// 获取从库上下文对线
    /// </summary>
    /// <returns></returns>
    DbContext GetSlaveDbContext();

    /// <summary>
    /// 返回第一个实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <returns></returns>
    Task<T> FirstOrDefaultAsync<T>() where T : class, new();

    /// <summary>
    /// 根据主键查找实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="keyValues">主键</param>
    /// <returns></returns>
    ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new();


    /// <summary>
    /// 根据主键查找实体
    /// </summary>
    /// <param name="type">实体类型</param>
    /// <param name="keys">主键</param>
    /// <returns></returns>
    ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys);

    /// <summary>
    /// 查找实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="predicate">表达式</param>
    /// <returns></returns>
    Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new();

    /// <summary>
    /// 查询列表
    /// </summary>
    Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new();

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Pagination<T>> ToPageAsync<T>(PageInput input, Expression<Func<T, bool>> predicate = null) where T : class, new();

    /// <summary>
    /// sql查询
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <param name="args">参数</param>
    /// <typeparam name="T">实体类型</typeparam>
    /// <remarks>参数化:select * from blog where id = {0} </remarks>
    Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class, new();


    /// <summary>
    /// sql查询
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <typeparam name="T">实体类型</typeparam>
    /// <remarks>参数化:$"select * from blog where id = {value}" </remarks>
    Task<List<T>> ToListAsync<T>(FormattableString sql) where T : class, new();

    /// <summary>
    /// sql查询
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <param name="args">参数</param>
    /// <typeparam name="T">实体类型</typeparam>
    /// <remarks>参数化:select top 1 from blog where id = {0} </remarks>
    Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class, new();

    /// <summary>
    /// sql查询
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <typeparam name="T">实体类型</typeparam>
    /// <remarks>参数化:$"select top 1 from blog where id = {value}" </remarks>
    Task<T> FirstOrDefaultAsync<T>(FormattableString sql) where T : class, new();
}
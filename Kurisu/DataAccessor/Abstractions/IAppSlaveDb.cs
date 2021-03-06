using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 从库接口,读操作
    /// </summary>
    public interface IAppSlaveDb : IDbService
    {
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
        /// <returns>总数<see cref="int"/> 当前页<see cref="IEnumerable{T}"/></returns>
        Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new();
    }
}
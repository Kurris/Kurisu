using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 写操作
    /// </summary>
    public interface IWriteOperation
    {
        /// <summary>
        /// 保存一个实体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        ValueTask SaveAsync(object entity);

        /// <summary>
        /// 保存多个实体
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        ValueTask SaveAsync(IEnumerable<object> entities);

        /// <summary>
        /// 添加一个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        ValueTask AddAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 添加一组实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">一组实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task AddAsync<T>(IEnumerable<T> entities) where T : class, new();

        /// <summary>
        /// 更新一个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实例</param>
        /// <param name="updateAll">全部更新</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new();

        /// <summary>
        /// 更新一组实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">一组实例</param>
        /// <param name="updateAll">全部更新</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task UpdateAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new();


        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="setPredicates">set条件</param>
        /// <param name="wherePredicate">where条件</param>
        /// <param name="keyValues">参数</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task UpdateAsync<T>(IEnumerable<Expression<Func<T, bool>>> setPredicates, Expression<Func<T, bool>> wherePredicate, IDictionary<string, object> keyValues = default) where T : class, new();

        /// <summary>
        /// 删除一个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 删除一组实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">一组实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync<T>(IEnumerable<T> entities) where T : class, new();

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="keyValue">主键</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync<T>(object keyValue) where T : class, new();

        /// <summary>
        /// 删除一组实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="keyValues">一组主键</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync<T>(IEnumerable<object> keyValues) where T : class, new();


        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">where表达式</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new();
    }
}
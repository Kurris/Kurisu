using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Internal;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 主库接口
    /// </summary>
    public interface IMasterDb : IDb
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        public AppDbContext<IMasterDb> DbContext { get; }

        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="strSql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task<int> RunSqlAsync(string strSql, IDictionary<string, object> keyValues = null);

        /// <summary>
        /// 执行SQL(插值)
        /// </summary>
        /// <param name="strSql">内插sql字符串</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task<int> RunSqlInterAsync(FormattableString strSql);

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <param name="keyValues">参数</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task ExecProcAsync(string procName, IDictionary<string, object> keyValues = null);

        /// <summary>
        /// 保存一个实体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        ValueTask<T> SaveAsync<T>(object entity) where T : class, new();

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ValueTask SaveAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 保存多个
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<T>> SaveAsync<T>(IEnumerable<object> entities) where T : class, new();

        /// <summary>
        /// 保存多个
        /// </summary>
        /// <param name="entities"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new();

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
    }
}
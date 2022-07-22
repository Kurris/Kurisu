using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Internal;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 主库接口
    /// </summary>
    public interface IAppMasterDb : IDbService
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        /// <returns></returns>
        public AppDbContext<IAppMasterDb> GetMasterDbContext();

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
        Task<int> RunSqlAsync(string sql, params object[] args);

        /// <summary>
        /// 执行SQL(插值)
        /// </summary>
        /// <param name="strSql">内插sql字符串</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task<int> RunSqlInterAsync(FormattableString strSql);

        #endregion

        #region save

        /// <summary>
        /// 保存一个实体
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
        /// 保存多个
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        ValueTask SaveAsync(IEnumerable<object> entities);

        /// <summary>
        /// 保存多个
        /// </summary>
        /// <param name="entities"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new();

        #endregion

        #region insert

        /// <summary>
        /// 添加一个实体
        /// </summary>
        /// <param name="entity">实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        ValueTask InsertAsync(object entity);

        ValueTask<object> InsertReturnIdentityAsync(object entity);

        /// <summary>
        /// 添加一个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        ValueTask InsertAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 添加一组实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">一组实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class, new();

        #endregion

        #region update

        /// <summary>
        /// 更新一个实体
        /// </summary>
        /// <param name="entity">实例</param>
        /// <param name="updateAll">全部更新</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task UpdateAsync(object entity, bool updateAll = false);

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
        Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new();

        #endregion


        #region delete

        /// <summary>
        /// 删除一个实体
        /// </summary>
        /// <param name="entity">实例</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteAsync(object entity);

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
        Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new();

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="keyValue">主键</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task DeleteByIdAsync<T>(object keyValue) where T : class, new();

        #endregion
    }
}
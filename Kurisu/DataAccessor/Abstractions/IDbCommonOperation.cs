using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 数据库操作通用接口
    /// </summary>
    public interface IDbCommonOperation
    {
        /// <summary>
        /// 查找主键和主键值
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实例</param>
        /// <returns>主键和值 <see cref="IDictionary{string, object}"/> </returns>
        IDictionary<string, object> FindPrimaryKeyValue<T>(T t) where T : class, new();


        /// <summary>
        /// 查找主键和主键值
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实例</param>
        /// <returns>主键和值 <see cref="(string,object)"/> </returns>
        (string key, object value) FindFirstPrimaryKeyValue<T>(T t) where T : class, new();

        /// <summary>
        /// 查找主键和主键值
        /// </summary>
        /// <param name="entity">实例</param>
        /// <returns>主键和值 <see cref="(string,int)"/> </returns>
        (string key, object value) FindFirstPrimaryKeyValue(object entity);

        /// <summary>
        /// 查找表和主键
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="(string table, IEnumerable{string} keys) "/></returns>
        (string table, IEnumerable<string> keys) FindPrimaryKeyWithTable<T>() where T : class, new();

        /// <summary>
        /// 查找主键
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>主键名称 <see cref="IEnumerable{String}"/></returns>
        IEnumerable<string> FindPrimaryKey<T>() where T : class, new();

        /// <summary>
        /// 转成IQueryable
        /// </summary>
        /// <remarks>
        /// 如果表达式为 <see cref="null"/>, 则返回无条件的 IQueryable
        /// </remarks>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <returns><see cref="IQueryable"/></returns>
        IQueryable<T> AsQueryable<T>(Expression<Func<T, bool>> predicate) where T : class, new();

        /// <summary>
        /// 转成IQueryable
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns><see cref="IQueryable{T}"/></returns>
        IQueryable<T> AsQueryable<T>() where T : class, new();

        /// <summary>
        /// 当前实体转成无跟踪
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns><see cref="IQueryable{T}"/></returns>
        IQueryable<T> AsNoTracking<T>() where T : class, new();

        /// <summary>
        /// DbContext 转成无跟踪
        /// </summary>
        /// <returns></returns>
        IDbCommonOperation AsNoTracking();

        /// <summary>
        /// DbContext 转成无跟踪,如果存在连表,那么关联表的数据将会以key为准,相同key将会使用同一个外表实例
        /// </summary>
        /// <code>
        /// var users = await _dbContext.Users.AsNoTrackingWithIdentityResolution()
        ///                          .Include(a => a.Role).ToListAsync();
        ///
        ///  Users中可能有相同的Role,那么Role就会以key为准,公用一个实例
        /// </code>
        /// <returns></returns>
        IDbCommonOperation AsNoTrackingWithIdentityResolution();

        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="strSql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task RunSqlAsync(string strSql, IDictionary<string, object> keyValues = null);

        /// <summary>
        /// 执行SQL(插值)
        /// </summary>
        /// <param name="strSql">内插sql字符串</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task RunSqlInterAsync(FormattableString strSql);

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <param name="keyValues">参数</param>
        /// <returns>返回受影响行<see cref="int"/></returns>
        Task ExecProcAsync(string procName, IDictionary<string, object> keyValues = null);
    }
}
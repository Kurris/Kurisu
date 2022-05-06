using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 主库接口
    /// </summary>
    public interface IMasterDbService : IWriteOperation, ISlaveDbService
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        public DbContext DbContext { get; }

        /// <summary>
        /// 转成IQueryable
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <returns><see cref="IQueryable{T}"/></returns>
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
        IMasterDbService AsNoTracking();

        /// <summary>
        /// DbContext 转成无跟踪,如果存在连表,那么关联表的数据将会以key为准,相同key将会使用同一个外表实例
        /// </summary>
        /// <code>
        /// var users = await _dbContext.Users.AsNoTrackingWithIdentityResolution()
        ///                          .Include(a => a.Role).ToListAsync();
        ///
        /// Users中可能有相同的Role,那么Role就会以key为准,公用一个实例
        /// </code>
        /// <returns></returns>
        IMasterDbService AsNoTrackingWithIdentityResolution();

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
    }
}
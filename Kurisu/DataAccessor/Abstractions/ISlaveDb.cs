using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.Common.Dto;
using Kurisu.DataAccessor.Internal;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 从库接口,读操作
    /// </summary>
    public interface ISlaveDb : IDb
    {
        public AppDbContext<ISlaveDb> SlaveDbContext { get; }

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
        Task<IEnumerable<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new();


        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageSize">页行数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns>总数<see cref="int"/> 当前页<see cref="IEnumerable{T}"/></returns>
        Task<Pagination<T>> ToPageAsync<T>(int pageIndex, int pageSize) where T : class, new();

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <param name="pageSize">页行数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns>总数<see cref="int"/> 当前页<see cref="IEnumerable{T}"/></returns>
        Task<Pagination<T>> ToPageAsync<T>(Expression<Func<T, bool>> predicate, int pageIndex, int pageSize) where T : class, new();


        /// <summary>
        /// 获取DataTable
        /// </summary>
        /// <param name="sql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns><see cref="DataTable"/></returns>
        Task<DataTable> GetTableAsync(string sql, IDictionary<string, object> keyValues = null);


        /// <summary>
        /// 获取首行数据(需要对IDataReader数据在使用后及时释放)
        /// </summary>
        /// <param name="sql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns>首行<see cref="IDataReader"/></returns>
        Task<IDataReader> GetReaderAsync(string sql, IDictionary<string, object> keyValues = null);

        /// <summary>
        /// 获取首行首列值
        /// </summary>
        /// <param name="sql">sq</param>
        /// <param name="keyValues">参数</param>
        /// <returns>首行首列<see cref="object"/></returns>
        Task<object> GetScalarAsync(string sql, IDictionary<string, object> keyValues = null);
    }
}
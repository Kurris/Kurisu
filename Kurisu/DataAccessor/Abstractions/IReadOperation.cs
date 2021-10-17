using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 读操作
    /// </summary>
    public interface IReadOperation
    {
        /// <summary>
        /// 返回第一个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>实体<see cref="{T}"/></returns>
        Task<T> FindFirstAsync<T>() where T : class, new();

        /// <summary>
        /// 根据主键查找实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="KeyValue">主键</param>
        /// <returns>实体<see cref="{T}"/></returns>
        ValueTask<T> FindAsync<T>(params object[] keyValues) where T : class, new();


        /// <summary>
        /// 根据主键查找实体
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="keys">主键</param>
        /// <returns></returns>
        ValueTask<object> FindAsync(Type type, params object[] keys);

        /// <summary>
        /// 查找实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <returns>实体<see cref="{T}"/></returns>
        Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new();


        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageSize">页行数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns>总数<see cref="int"/> 当前页<see cref="IEnumerable{T}"/></returns>
        Task<Pagination<T>> FindListAsync<T>(int pageIndex, int pageSize) where T : class, new();

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">表达式</param>
        /// <param name="pageSize">页行数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns>总数<see cref="int"/> 当前页<see cref="IEnumerable{T}"/></returns>
        Task<Pagination<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate, int pageIndex, int pageSize) where T : class, new();


        /// <summary>
        /// 获取DataTable
        /// </summary>
        /// <param name="strSql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns><see cref="DataTable"/></returns>
        Task<DataTable> GetTableAsync(string strSql, IDictionary<string, object> keyValues = null);


        /// <summary>
        /// 获取首行数据(需要对IDataReader数据在使用后及时释放)
        /// </summary>
        /// <param name="strSql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns>首行<see cref="IDataReader"/></returns>
        Task<IDataReader> GetReaderAsync(string strSql, IDictionary<string, object> keyValues = null);

        /// <summary>
        /// 获取首行首列值
        /// </summary>
        /// <param name="strSql">sql字符串</param>
        /// <param name="keyValues">参数</param>
        /// <returns>首行首列<see cref="object"/></returns>
        Task<object> GetScalarAsync(string strSql, IDictionary<string, object> keyValues = null);
    }
}
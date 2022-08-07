using System.Linq;

namespace Kurisu.DataAccessor.Functions.Default.Abstractions
{
    /// <summary>
    /// Db基础服务
    /// </summary>
    public interface IBaseDbService
    {
        /// <summary>
        /// 获取EF <see cref="IQueryable"/> 
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns><see cref="IQueryable"/></returns>
        IQueryable<T> Queryable<T>() where T : class, new();
    }
}
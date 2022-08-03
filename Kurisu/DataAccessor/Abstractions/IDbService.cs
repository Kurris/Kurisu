using System.Linq;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// Db服务
    /// </summary>
    public interface IDbService
    {
        /// <summary>
        /// 获取EF <see cref="IQueryable"/> 
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns><see cref="IQueryable"/></returns>
        IQueryable<T> Queryable<T>() where T : class, new();
    }
}
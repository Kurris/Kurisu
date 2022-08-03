using System.Linq;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// 读写分离数据访问服务
    /// </summary>
    public interface IAppDbService : IAppMasterDb
    {
        /// <summary>
        ///  获取EF <see cref="IQueryable"/> ，并且指定使用主从库
        /// </summary>
        /// <param name="useMasterDb"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new();
    }
}
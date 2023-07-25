using System.Linq;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;

namespace Kurisu.DataAccessor.Functions.Default.Abstractions;

/// <summary>
/// 读写分离数据访问服务
/// </summary>
public interface IAppDbService : IAppMasterDb
{
    /// <summary>
    ///  获取EF <see cref="IQueryable"/>,并且指定使用主从库
    /// </summary>
    /// <param name="useMasterDb">是否使用主库</param>
    /// <typeparam name="T">查询实体</typeparam>
    /// <returns></returns>
    IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new();
}
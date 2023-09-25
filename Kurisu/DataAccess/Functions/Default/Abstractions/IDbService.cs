using System.Linq;

namespace Kurisu.DataAccess.Functions.Default.Abstractions;

/// <summary>
/// 数据访问服务
/// </summary>
public interface IDbService : IDbWrite
{
    /// <summary>
    ///  获取<see cref="IQueryable"/>
    /// </summary>
    /// <param name="useMasterDb">是否使用主库</param>
    /// <typeparam name="T">查询实体</typeparam>
    /// <returns></returns>
    IQueryable<T> AsQueryable<T>(bool useWriteDb) where T : class, new();
}
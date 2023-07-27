using System.Linq;

namespace Kurisu.DataAccessor.Functions.Default.Abstractions;

/// <summary>
/// Db基础服务
/// </summary>
public interface IBaseDbService
{
    /// <summary>
    /// 获取<see cref="IQueryable"/>
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <returns><see cref="IQueryable"/>可查询表达式</returns>
    IQueryable<T> AsQueryable<T>() where T : class, new();
}
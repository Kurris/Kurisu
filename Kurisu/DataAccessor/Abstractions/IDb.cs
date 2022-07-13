using System.Linq;

namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// Db服务
    /// </summary>
    public interface IDb
    {
        IQueryable<T> Queryable<T>() where T : class, new();
    }
}
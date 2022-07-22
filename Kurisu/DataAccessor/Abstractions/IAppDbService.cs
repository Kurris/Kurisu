using System.Linq;

namespace Kurisu.DataAccessor.Abstractions
{
    public interface IAppDbService : IAppMasterDb, IAppSlaveDb
    {
        IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new();
    }
}
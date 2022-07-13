using System.Linq;
using Kurisu.DataAccessor.Abstractions;

namespace Kurisu.DataAccessor.Internal
{
    public class ReadImplementation : DbOperation<ISlaveDb>, ISlaveDb
    {
        internal ReadImplementation(IAppDbContext dbContext) : base(dbContext)
        {
        }

        public AppDbContext<ISlaveDb> SlaveDbContext => DbContext;

        public IQueryable<T> Queryable<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsQueryable();
        }
    }
}
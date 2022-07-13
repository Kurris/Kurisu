using System.Linq;
using Kurisu.DataAccessor.Abstractions;

namespace Kurisu.DataAccessor.Internal
{
    public class WriteImplementation : DbOperation<IMasterDb>, IMasterDb
    {
        internal WriteImplementation(IAppDbContext dbContext) : base(dbContext)
        {
        }

        public AppDbContext<IMasterDb> SlaveDbContext => DbContext;

        public IQueryable<T> Queryable<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsQueryable();
        }
    }
}
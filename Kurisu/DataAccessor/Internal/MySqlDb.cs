using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Internal
{
    internal class MySqlDb : DbOperationImplementation
    {
        internal MySqlDb(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
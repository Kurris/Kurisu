using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Internal
{
    public class MySqlDb : DbOperationImplementation
    {
        public MySqlDb(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
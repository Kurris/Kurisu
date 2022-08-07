using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.ReadWriteSplit.DbContexts
{
    internal class WriteAppDbContext : DefaultAppDbContext
    {
        public WriteAppDbContext(DbContextOptions<DefaultAppDbContext> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver
            , IOptions<DbSetting> dbOptions)
            : base(options, defaultValuesOnSaveChangesResolver, queryFilterResolver, dbOptions)
        {

        }
    }
}

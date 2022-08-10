using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts
{
    public class UnitOfWorkDbContext : DefaultAppDbContext<IAppMasterDb>, IUnitOfWorkDbContext
    {
        public UnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver)
            : base(options, defaultValuesOnSaveChangesResolver, queryFilterResolver)
        {
        }

        public bool IsAutomaticSaveChanges { get; set; }

        public DbContext GetUnitOfWorkDbContext() => this;
    }
}
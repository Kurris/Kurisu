using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Resolvers.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.MultiTenant.DbContexts
{
    /// <summary>
    /// 多租户,读
    /// </summary>
    public class MultiTenantReadDbContext : DefaultAppDbContext<IAppSlaveDb>, ITenantId
    {
        public MultiTenantReadDbContext(DbContextOptions<DefaultAppDbContext<IAppSlaveDb>> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver
            , IModelConfigurationSourceResolver modelConfigurationSourceResolver
            , ICurrentTenantInfoResolver currentTenantInfoResolver)
            : base(options, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver)
        {
            TenantId = currentTenantInfoResolver.GetTenantId();
        }

        /// <summary>
        /// 租户id值
        /// </summary>
        public int TenantId { get; set; }
    }
}
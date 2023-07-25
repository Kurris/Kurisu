using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.MultiTenant.DbContexts;

/// <summary>
/// 工作单元多租户,写
/// </summary>
public class MultiTenantWriteInUnitOfWorkDbContext : UnitOfWorkDbContext, ITenantId
{
    public MultiTenantWriteInUnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
        , IOptions<KurisuDataAccessorBuilderSetting> builderOptions
        , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
        , IQueryFilterResolver queryFilterResolver
        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
        , ICurrentTenantInfoResolver currentTenantInfoResolver)
        : base(options, builderOptions, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver)
    {
        TenantId = currentTenantInfoResolver.GetTenantId();
    }

    /// <summary>
    /// 租户id值
    /// </summary>
    public int TenantId { get; set; }
}
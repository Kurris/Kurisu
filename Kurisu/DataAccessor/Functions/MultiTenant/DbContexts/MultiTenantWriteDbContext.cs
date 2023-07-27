using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.MultiTenant.DbContexts;

/// <summary>
/// 多租户,写
/// </summary>
public class MultiTenantWriteDbContext : DefaultAppDbContext<IDbWrite>, ITenantId
{
    public MultiTenantWriteDbContext(DbContextOptions<DefaultAppDbContext<IDbWrite>> options
        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
        , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
        , IQueryFilterResolver queryFilterResolver
        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
        , ICurrentTenantInfoResolver currentTenantInfoResolver
        , ICurrentUserInfoResolver currentUserInfoResolver)
        : base(options, builderOptions, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver, currentUserInfoResolver)
    {
        TenantId = currentTenantInfoResolver.GetTenantId();
    }

    /// <summary>
    /// 租户id值
    /// </summary>
    public int TenantId { get; set; }
}
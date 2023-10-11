//using Kurisu.Authentication.Abstractions;
//using Kurisu.DataAccess.Entity;
//using Kurisu.DataAccess.Functions.Default;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Kurisu.DataAccess.Functions.MultiTenant;

///// <summary>
///// 多租户,读
///// </summary>
//public class MultiTenantReadDbContext : DefaultAppDbContext<IDbRead>, ITenantId
//{
//    public MultiTenantReadDbContext(DbContextOptions<DefaultAppDbContext<IDbRead>> options
//        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
//        , IQueryFilterResolver queryFilterResolver
//        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
//        , ICurrentTenantInfoResolver currentTenantInfoResolver
//        , ICurrentUserInfoResolver currentUserInfoResolver)
//        : base(options, builderOptions, queryFilterResolver, modelConfigurationSourceResolver, currentUserInfoResolver)
//    {
//        TenantId = currentTenantInfoResolver.GetTenantId();
//    }

//    /// <summary>
//    /// 租户id值
//    /// </summary>
//    public int TenantId { get; set; }
//}
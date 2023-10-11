//using Kurisu.Authentication.Abstractions;
//using Kurisu.DataAccess.Entity;
//using Kurisu.DataAccess.Functions.Default;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Kurisu.DataAccess.Functions.UnitOfWork.DbContexts;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Kurisu.DataAccess.Functions.MultiTenant;

///// <summary>
///// 工作单元多租户,写
///// </summary>
//public class MultiTenantWriteInUnitOfWorkDbContext : UnitOfWorkDbContext, ITenantId
//{
//    public MultiTenantWriteInUnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IDbWrite>> options
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
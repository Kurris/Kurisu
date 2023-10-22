//using Kurisu.Authentication.Abstractions;
//using Kurisu.DataAccess.Functions.Default;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Kurisu.DataAccess.Functions.UnitOfWork.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace Kurisu.DataAccess.Functions.UnitOfWork.DbContexts;

///// <summary>
///// 工作单元数据库上下文
///// </summary>
//public class UnitOfWorkDbContext : DefaultAppDbContext<IDbWrite>, IUnitOfWorkDbContext
//{
//    public UnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IDbWrite>> options
//        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
//        , IQueryFilterResolver queryFilterResolver
//        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
//        , ICurrentUserInfoResolver currentUserInfoResolver)
//        : base(options, builderOptions, queryFilterResolver, modelConfigurationSourceResolver, currentUserInfoResolver)
//    {
//    }

//    /// <summary>
//    /// 是否自动提交
//    /// </summary>
//    public bool IsAutomaticSaveChanges { get; set; }
//}
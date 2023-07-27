using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts;

/// <summary>
/// 工作单元数据库上下文
/// </summary>
public class UnitOfWorkDbContext : DefaultAppDbContext<IDbWrite>, IUnitOfWorkDbContext
{
    public UnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IDbWrite>> options
        , IOptions<KurisuDataAccessorSettingBuilder> builderOptions
        , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
        , IQueryFilterResolver queryFilterResolver
        , IModelConfigurationSourceResolver modelConfigurationSourceResolver
        , ICurrentUserInfoResolver currentUserInfoResolver)
        : base(options, builderOptions, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver, currentUserInfoResolver)
    {
    }

    /// <summary>
    /// 是否自动提交
    /// </summary>
    public bool IsAutomaticSaveChanges { get; set; }
}
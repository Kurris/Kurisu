using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts
{
    /// <summary>
    /// 工作单元数据库上下文
    /// </summary>
    public class UnitOfWorkDbContext : DefaultAppDbContext<IAppMasterDb>, IUnitOfWorkDbContext
    {
        public UnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
            , IOptions<KurisuDataAccessorBuilderSetting> builderOptions
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver
            , IModelConfigurationSourceResolver modelConfigurationSourceResolver)
            : base(options, builderOptions, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver)
        {
        }

        /// <summary>
        /// 是否自动提交
        /// </summary>
        public bool IsAutomaticSaveChanges { get; set; }

        /// <summary>
        /// 获取工作单元所在的数据库上下文
        /// </summary>
        /// <returns></returns>
        public DbContext GetUnitOfWorkDbContext() => this;
    }
}
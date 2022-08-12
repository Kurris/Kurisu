using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Kurisu.DataAccessor.Resolvers.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts
{
    /// <summary>
    /// 工作单元数据库上下文
    /// </summary>
    public class UnitOfWorkDbContext : DefaultAppDbContext<IAppMasterDb>, IUnitOfWorkDbContext
    {
        public UnitOfWorkDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver
            , IModelConfigurationSourceResolver modelConfigurationSourceResolver)
            : base(options, defaultValuesOnSaveChangesResolver, queryFilterResolver, modelConfigurationSourceResolver)
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
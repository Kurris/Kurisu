using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions.Setting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.Default.DbContexts
{
    /// <summary>
    /// AppDbContext 程序默认DbContext
    /// </summary>
    public class DefaultAppDbContext : DbContext
    {
        private readonly IDefaultValuesOnSaveChangesResolver _defaultValuesOnSaveChangesResolver;
        private readonly IQueryFilterResolver _queryFilterResolver;

        public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver
            , IOptions<DbSetting> dbOptions) : base(options)
        {
            _defaultValuesOnSaveChangesResolver = defaultValuesOnSaveChangesResolver;
            _queryFilterResolver = queryFilterResolver;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityTypes = App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false));
            foreach (var entityType in entityTypes)
            {
                //避免导航属性重复加载
                if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var builder = modelBuilder.Entity(entityType);
                _queryFilterResolver.HandleQueryFilter(builder, entityType);
            }

            base.OnModelCreating(modelBuilder);
        }

        #region SaveChanges

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            GenerateDefaultValues();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            GenerateDefaultValues();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            GenerateDefaultValues();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            GenerateDefaultValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        #endregion

        /// <summary>
        /// 生成默认值
        /// </summary>
        protected virtual void GenerateDefaultValues()
        {
            _defaultValuesOnSaveChangesResolver.OnSaveChanges(this);
        }
    }
}
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.Default.DbContexts
{
    /// <summary>
    /// AppDbContext 程序默认DbContext
    /// </summary>
    public class DefaultAppDbContext<TDbService> : DbContext where TDbService : IBaseDbService
    {
        private readonly IDefaultValuesOnSaveChangesResolver _defaultValuesOnSaveChangesResolver;
        private readonly IQueryFilterResolver _queryFilterResolver;

        public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<TDbService>> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver) : base(options)
        {
            _defaultValuesOnSaveChangesResolver = defaultValuesOnSaveChangesResolver;
            _queryFilterResolver = queryFilterResolver;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //定义TableAttribute,并且非继承
            var entityTypes = App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false));

            foreach (var entityType in entityTypes)
            {
                //避免导航属性重复加载
                if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var builder = modelBuilder.Entity(entityType);
                _queryFilterResolver?.HandleQueryFilter(builder, entityType);
            }

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            GenerateDefaultValues();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            GenerateDefaultValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


        /// <summary>
        /// 生成默认值
        /// </summary>
        protected virtual void GenerateDefaultValues()
        {
            _defaultValuesOnSaveChangesResolver.OnSaveChanges(this);
        }
    }
}
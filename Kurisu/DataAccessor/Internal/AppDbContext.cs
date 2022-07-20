using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.DataAccessor.Internal
{
    /// <summary>
    /// AppDbContext 程序默认DbContext
    /// </summary>
    /// <typeparam name="TIDb"></typeparam>
    public class AppDbContext<TIDb> : DbContext, IAppDbContext where TIDb : IDb
    {
        public AppDbContext(DbContextOptions<AppDbContext<TIDb>> options) : base(options)
        {
        }

        public bool IsAutomaticSaveChanges { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entities = App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false));
            foreach (var entityType in entities)
            {
                if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                modelBuilder.Entity(entityType);
            }

            base.OnModelCreating(modelBuilder);
        }

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

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
        {
            GenerateDefaultValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// 生成默认
        /// </summary>
        private void GenerateDefaultValues()
        {
            var httpContext = this.GetService<IHttpContextAccessor>().HttpContext;
            int sub = int.Parse((httpContext?.User.Identity as ClaimsIdentity)?.FindFirst("sub")?.Value ?? "0");

            var entities = this.ChangeTracker.Entries();
            foreach (var entity in entities)
            {
                switch (entity.State)
                {
                    case EntityState.Modified:
                        entity.Property(nameof(BaseEntity<object>.Updater)).CurrentValue = sub;
                        entity.Property(nameof(BaseEntity<object>.UpdateTime)).CurrentValue = DateTime.Now;
                        break;
                    case EntityState.Added:
                        entity.Property(nameof(BaseEntity<object>.Creator)).CurrentValue = sub;
                        entity.Property(nameof(BaseEntity<object>.CreateTime)).CurrentValue = DateTime.Now;
                        break;
                }
            }
        }
    }
}
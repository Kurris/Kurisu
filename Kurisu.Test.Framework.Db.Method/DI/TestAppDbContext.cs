using System;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Entity;
using Microsoft.EntityFrameworkCore;
using weather;

namespace Kurisu.Test.Framework.Db.Method.DI
{
    /// <summary>
    /// AppDbContext 程序默认DbContext
    /// </summary>
    public class TestAppDbContext : DbContext
    {
        public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options)
        {
        }

        public DbSet<WeatherForecast> WeatherForecasts { get; set; }


        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            GenerateDefaultValues();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }


        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
        {
            GenerateDefaultValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// 生成默认
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        protected void GenerateDefaultValues()
        {
            var sub = 1;

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

        public bool IsAutomaticSaveChanges { get; set; } = true;
        public DbContext GetUnitOfWorkDbContext() => this;
    }
}
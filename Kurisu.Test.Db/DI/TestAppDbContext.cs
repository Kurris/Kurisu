using System;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.EntityFrameworkCore;
using weather;

namespace Kurisu.Test.Db.DI
{
    /// <summary>
    /// AppDbContext 程序默认DbContext
    /// </summary>
    /// <typeparam name="TIDb"></typeparam>
    public class TestAppDbContext : DbContext, IUnitOfWorkDbContext
    {
        public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options)
        {
        }

        public bool IsAutomaticSaveChanges { get; set; } = true;
        public DbContext GetUnitOfWorkDbContext() => this;


        public DbSet<WeatherForecast> WeatherForecasts { get; set; }


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
    }
}
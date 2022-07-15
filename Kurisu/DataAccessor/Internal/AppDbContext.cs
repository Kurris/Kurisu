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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kurisu.DataAccessor.Internal
{
    public class AppDbContext<TIDb> : DbContext, IAppDbContext where TIDb : IDb
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext<TIDb>> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entities = App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false));
            foreach (var entityType in entities)
            {
                if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                modelBuilder.Entity(entityType);
            }

            //var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            //    v => DateTime.Parse(v.ToString("yyyy-MM-dd HH:mm:ss")),
            //    v => DateTime.Parse(v.ToString("yyyy-MM-dd HH:mm:ss"))
            //);

            //foreach (var item in modelBuilder.Model.GetEntityTypes())
            //{
            //    foreach (var prop in item.GetProperties().Where(x => x.ClrType == typeof(DateTime) || x.ClrType == typeof(DateTime?)))
            //    {
            //        prop.SetValueConverter(dateTimeConverter);
            //    }
            //}

            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entities = this.ChangeTracker.Entries();
            foreach (var entity in entities)
            {
                int sub = int.Parse((_httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity)?.FindFirst("sub")?.Value ?? "0");

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
                    default:
                        break;
                }
            }


            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kurisu.DataAccessor.Internal
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in App.ActiveTypes.Where(x => x.IsDefined(typeof(TableAttribute), false)))
            {
                if (modelBuilder.Model.GetEntityTypes().Any(x => x.Name.Equals(entityType.FullName))) continue;

                modelBuilder.Entity(entityType);
            }

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => DateTime.Parse(v.ToString("yyyy-MM-dd HH:mm:ss")),
                v => DateTime.Parse(v.ToString("yyyy-MM-dd HH:mm:ss"))
            );

            foreach (var item in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var prop in item.GetProperties().Where(x => x.ClrType == typeof(DateTime) || x.ClrType == typeof(DateTime?)))
                {
                    prop.SetValueConverter(dateTimeConverter);
                    prop.SetMaxLength(14);
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException e)
            {
            }

            return await Task.FromResult(0);
        }
    }
}
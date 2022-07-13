using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kurisu.DataAccessor.Internal
{
    public class AppDbContext<TIDb> : DbContext, IAppDbContext where TIDb : IDb
    {
        public AppDbContext(DbContextOptions<AppDbContext<TIDb>> options) : base(options)
        {
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
    }
}
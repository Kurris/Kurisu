using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebApiDemo
{
    public class DesignMigrations : IDesignTimeDbContextFactory<DefaultAppDbContext>
    {
        public DefaultAppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json").Build();

            var dbSetting = configuration.GetSection("DbSetting").Get<DbSetting>();

            var dbBuilder = new DbContextOptionsBuilder<DefaultAppDbContext>();

            var connectionString = dbSetting.DefaultConnectionString;
            var mysqlVersion = MySqlServerVersion.LatestSupportedServerVersion;
            dbBuilder.UseMySql(connectionString, mysqlVersion, options => { options.MigrationsAssembly("WebApiDemo"); });

            return new DefaultAppDbContext(dbBuilder.Options, null, null);
        }
    }
}
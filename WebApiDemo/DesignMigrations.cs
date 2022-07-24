using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebApiDemo
{
    public class DesignMigrations : IDesignTimeDbContextFactory<DefaultAppDbContext<IAppMasterDb>>
    {
        public DefaultAppDbContext<IAppMasterDb> CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json").Build();
            var dbSetting = configuration.GetSection("DbSetting").Get<DbSetting>();


            var dbBuilder = new DbContextOptionsBuilder<DefaultAppDbContext<IAppMasterDb>>();
            dbBuilder.UseMySql(dbSetting.DefaultConnectionString, MySqlServerVersion.LatestSupportedServerVersion, options =>
            {
                options.MigrationsAssembly("WebApiDemo");
            });

            return new DefaultAppDbContext<IAppMasterDb>(dbBuilder.Options);
        }
    }
}

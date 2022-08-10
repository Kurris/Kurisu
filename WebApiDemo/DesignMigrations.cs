using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
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

            var dbBuilder = new DbContextOptionsBuilder<DefaultAppDbContext<IAppMasterDb>>();

            var connectionString = dbSetting.DefaultConnectionString;
            var mysqlVersion = MySqlServerVersion.LatestSupportedServerVersion;
            dbBuilder.UseMySql(connectionString, mysqlVersion, options => { options.MigrationsAssembly("WebApiDemo"); });

            return new DefaultAppDbContext(dbBuilder.Options, null, null);
        }
    }


    public class DefaultAppDbContext : DefaultAppDbContext<IAppMasterDb>
    {
        public DefaultAppDbContext(DbContextOptions<DefaultAppDbContext<IAppMasterDb>> options
            , IDefaultValuesOnSaveChangesResolver defaultValuesOnSaveChangesResolver
            , IQueryFilterResolver queryFilterResolver)
            : base(options, defaultValuesOnSaveChangesResolver, queryFilterResolver)
        {
        }
    }
}
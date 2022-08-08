using Kurisu.DataAccessor;
using Kurisu.Test.Db.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Db
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json").Build();
            services.AddKurisuConfiguration(configuration);

            // var dbSetting = configuration.GetSection(nameof(DbSetting)).Get<DbSetting>();
            // services.AddKurisuDatabaseAccessor(options =>
            // {
            //     options.Timeout = dbSetting.Timeout;
            //     options.Version = dbSetting.Version;
            //     options.DefaultConnectionString = dbSetting.DefaultConnectionString;
            //     options.ReadConnectionStrings = dbSetting.ReadConnectionStrings;
            //     options.SlowSqlTime = dbSetting.SlowSqlTime;
            //     options.MigrationsAssembly = dbSetting.MigrationsAssembly;
            // });

            DbInjectHelper.InjectDbContext(services);
        }
    }
}
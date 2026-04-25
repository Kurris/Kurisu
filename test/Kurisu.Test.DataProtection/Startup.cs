using Kurisu.AspNetCore.Cache.Extensions;
using Kurisu.AspNetCore.DataProtection.Modules;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.SqlSugar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Test.DataProtection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            //.AddJsonFile("appsettings.Development.json")
            .Build();

        services.AddConfiguration(configuration);
        services.AddDependencyInjection();
        services.AddRedis();

        services.AddSqlSugar(DbType.MySqlConnector);

        var pack = new DataProtectionModule
        {
            Configuration = configuration
        };
        pack.ConfigureServices(services);
    }
}
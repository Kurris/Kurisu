using Kurisu.AspNetCore.Cache.Extensions;
using Kurisu.AspNetCore.DataProtection.Packs;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddSqlSugar();
        
        var pack = new DataProtectionPack
        {
            Configuration = configuration
        };
        pack.ConfigureServices(services);
    }
}
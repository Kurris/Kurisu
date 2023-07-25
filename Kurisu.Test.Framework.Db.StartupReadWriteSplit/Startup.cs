using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.Db.StartupReadWriteSplit;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json").Build();

        services.AddKurisuConfiguration(configuration);

        services.AddKurisuDatabaseAccessor()
            .AddKurisuReadWriteSplit();
    }
}
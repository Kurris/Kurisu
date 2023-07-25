using Kurisu.Test.Framework.Db.Method.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.Db.Method;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json").Build();

        services.AddKurisuConfiguration(configuration);

        DbInjectHelper.InjectDbContext(services);
    }
}
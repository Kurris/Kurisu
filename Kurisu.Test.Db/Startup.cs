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
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json").Build();

            services.AddKurisuConfiguration(configuration);

            DbInjectHelper.InjectDbContext(services);
        }
    }
}
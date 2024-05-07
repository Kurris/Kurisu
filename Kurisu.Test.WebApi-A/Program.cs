using Kurisu.Aspect;
using Kurisu.AspNetCore;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;
using Kurisu.AspNetCore.EventBus.Extensions;
using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.IdentityModel.Logging;
using SqlSugar;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        KurisuHost.Builder(args)
            //.EnableAppSettingsReload()
            .RunKurisu<Startup>();
    }
}

public class Startup : DefaultStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
        IdentityModelEventSource.ShowPII = true;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.ReplaceProxyService(App.DependencyServices.Select(x =>
        {
            var di = x.GetInterfaces().Where(p => p.IsAssignableTo(typeof(IDependency))).FirstOrDefault(p => p != typeof(IDependency));

            var lifetime = di == typeof(ISingletonDependency) ? ServiceLifetime.Singleton
                : di == typeof(IScopeDependency) ? ServiceLifetime.Scoped : ServiceLifetime.Transient;

            return new ReplaceProxyServiceItem
            {
                Lifetime = lifetime,
                Service = x
            };
        }));

        services.AddSqlSugar(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();

            return new List<ConnectionConfig>
            {
                new()
                {
                    ConfigId = "face",

                    ConnectionString = configuration.GetSection("SqlSugarOptions:FaceConnectionString").Value,
                    DbType = DbType.MySql,
                    InitKeyType = InitKeyType.Attribute,
                    IsAutoCloseConnection = true,

                    MoreSettings = new ConnMoreSettings
                    {
                        DisableNvarchar = true,
                    },
                }
            };
        });

        services.AddRedis();
        services.AddEventBus();
    }
}
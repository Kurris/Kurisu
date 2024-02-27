using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils.Extensions;
using Kurisu.SqlSugar.Extensions;
using Kurisu.Startup;
using Microsoft.IdentityModel.Logging;
using SqlSugar;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        KurisuHost.Run<Startup>(true, args);
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

        //services.AddRedis();
        services.AddSqlSugar(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();

            return new List<ConnectionConfig>
            {
                 new ConnectionConfig
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
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);

        //app.UseSnowFlakeInit();
    }
}
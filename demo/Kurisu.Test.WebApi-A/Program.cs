using AspectCore.Extensions.Hosting;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Cache.Extensions;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;
using Kurisu.AspNetCore.EventBus.Extensions;
using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils.Extensions;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.IdentityModel.Logging;
using static Kurisu.AspNetCore.Startup.KurisuHost;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        Builder(args)
            //.UseServiceContext()
            .UseDynamicProxy((_, configuration) => configuration.PropertyEnableInjectAttributeType = typeof(DiInjectAttribute))
            .RunKurisu<Startup>();
    }
}

public class Startup : DefaultStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
        IdentityModelEventSource.ShowPII = true;
    }

    /// <inheritdoc />
    // public override void ConfigureStartupOptions(StartupOptions options)
    // {
    //     var n = SnowFlakeHelper.Instance.NextId();
    //
    //     options.DocumentOptions = new DocumentOptions
    //     {
    //         Authority = "http://192.168.199.124:5000",
    //         ClientId = "98958aa485d38412",
    //         ClientSecret = "52589e011f02fe19",
    //         Scopes = new Dictionary<string, string>
    //         {
    //             ["openid"] = "身份id",
    //             ["profile"] = "个人信息",
    //             ["yikatong"] = "API资源"
    //         }
    //     };
    // }
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        //services.AddRemoteCall(App.ActiveTypes);
        services.AddHttpContextAccessor();
        services.AddSqlSugar();
        services.AddEventBus();
        services.AddRedis();
        services.AddMemoryCache();
    }

    public override async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        try
        {
            base.Configure(app, env);

            //await app.UseSnowFlakeDistributedInitializeAsync();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }
}
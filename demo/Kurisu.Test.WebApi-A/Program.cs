using AspectCore.Extensions.Hosting;
using Kurisu.Api;
using Kurisu.AspNetCore;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Cache.Extensions;
using Kurisu.AspNetCore.EventBus.Extensions;
using Kurisu.AspNetCore.Startup;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.IdentityModel.Logging;
using static Kurisu.AspNetCore.Startup.KurisuHost;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        Builder(args)
            .ConfigureServices(collection => collection.AddRemoteCall(App.ActiveTypes))
            .ConfigureAppConfiguration((_, config) => { config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "api.json"), optional: false, reloadOnChange: true); })
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

    public override async void Configure(IApplicationBuilder app)
    {
        try
        {
            var path = AppContext.BaseDirectory;
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var value = configuration.GetSection("aservice").Value;
            base.Configure(app);

            //await app.UseSnowFlakeDistributedInitializeAsync();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }
}
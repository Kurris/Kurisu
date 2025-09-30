using Kurisu.AspNetCore.Cache.Extensions;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;
using Kurisu.AspNetCore.EventBus.Extensions;
using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils;
using Kurisu.AspNetCore.Utils.Extensions;
using Mapster;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Logging;
using static Kurisu.AspNetCore.Startup.KurisuHost;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        var id = SnowFlakeHelper.Instance.NextId();
        var s = SnowFlakeHelper.ParseId(id);

        var a = (long)(DateTime.UtcNow - DateTimeOffset.UnixEpoch).TotalMilliseconds;
        var b = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var c = a == b;
        Builder(args)
            //.EnableAppSettingsReload()
            .RunKurisu<Startup>();
        //IStringLocalizer 
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
        //services.AddAop();
        services.AddMemoryCache();
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var a = TestEnumType.Wait.GetDisplay();
        var b = TestEnumType.Wait.GetDisplay("en");
        base.Configure(app, env);
    }
}
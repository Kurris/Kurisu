using Kurisu.AspNetCore;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;
using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils.Extensions;
using Kurisu.Test.WebApi_A.AutoReload;
using Microsoft.IdentityModel.Logging;

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
        //services.AddRemoteCall(App.ActiveTypes);
        services.AddSqlSugar();
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var a = TestEnumType.Wait.GetDescriptionEn();
        var b = TestEnumType.Wait.GetDescription();
        base.Configure(app, env);
    }
}
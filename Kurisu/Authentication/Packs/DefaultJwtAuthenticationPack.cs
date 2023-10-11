using Kurisu.Authentication.Settings;
using Kurisu.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Authentication.Packs;

/// <summary>
/// jwt pack
/// </summary>
public class DefaultJwtAuthenticationPack : BaseAppPack
{
    public override int Order => 2;

    public override bool IsEnable => false;

    public override bool IsBeforeUseRouting => false;

    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(JwtSetting)).Get<JwtSetting>();
        services.AddKurisuJwtAuthentication(setting, context => { });
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}

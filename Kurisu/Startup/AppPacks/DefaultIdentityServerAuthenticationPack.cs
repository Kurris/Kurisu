using Kurisu.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.AppPacks;

/// <summary>
/// identity server4默认pack
/// </summary>
public class DefaultIdentityServerAuthenticationPack : BaseAppPack
{
    public override int Order => 2;

    public override bool IsBeforeUseRouting => false;

    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(IdentityServerSetting)).Get<IdentityServerSetting>();
        services.AddKurisuOAuth2Authentication(setting);
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
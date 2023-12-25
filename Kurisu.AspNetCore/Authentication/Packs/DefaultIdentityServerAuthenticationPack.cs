using Kurisu.AspNetCore.Authentication.Options;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Packs;

/// <summary>
/// identity server4 pack
/// </summary>
public class DefaultIdentityServerAuthenticationPack : BaseAppPack
{
    public override int Order => 2;

    public override bool IsEnable => Configuration.GetSection(nameof(IdentityServerOptions)).Get<IdentityServerOptions>() != null;

    public override bool IsBeforeUseRouting => false;

    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(IdentityServerOptions)).Get<IdentityServerOptions>();
        services.AddOAuth2Authentication(setting);
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
using Kurisu.AspNetCore.Authentication.Options;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Packs;

/// <summary>
/// oauth2.0 pack
/// </summary>
public class DefaultOAuth2AuthenticationPack : BaseAppPack
{
    /// <summary>
    /// 执行顺序
    /// </summary>
    public override int Order => 2;

    /// <inheritdoc />
    public override bool IsEnable => Configuration.GetSection(nameof(IdentityServerOptions)).Get<IdentityServerOptions>() != null;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(IdentityServerOptions)).Get<IdentityServerOptions>();
        services.AddOAuth2Authentication(setting);
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
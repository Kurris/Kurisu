using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Authentication.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Modules;

/// <summary>
/// oauth2.0 module
/// </summary>
public class DefaultOAuth2AuthenticationModule : AppModule
{
    public override string Name => "OAuth2.0认证模块";

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
    public override void Configure(IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

}
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Authentication.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Modules;

/// <summary>
/// jwt module
/// </summary>
public class DefaultJwtAuthenticationModule : AppModule
{

    public override string Name => "jwt认证模块";

    /// <inheritdoc />
    public override int Order => 2;

    /// <inheritdoc />
    public override bool IsEnable => Configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>() != null;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();
        services.AddKurisuJwtAuthentication(setting);
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    
}
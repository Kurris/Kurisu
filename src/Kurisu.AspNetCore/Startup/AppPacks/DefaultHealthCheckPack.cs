using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.AppPacks;

/// <summary>
/// 健康检查
/// </summary>
public class DefaultHealthCheckPack : BaseAppPack
{
    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthChecks();
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app)
    {
        app.UseHealthChecks("/healthz");
    }
}

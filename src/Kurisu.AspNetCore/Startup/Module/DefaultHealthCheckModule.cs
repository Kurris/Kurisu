using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.Module;

/// <summary>
/// 健康检查
/// </summary>
public class DefaultHealthCheckModule : AppModule
{
    public override string Name => "健康检查模块";

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
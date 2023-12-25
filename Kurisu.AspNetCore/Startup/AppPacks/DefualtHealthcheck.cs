using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.AppPacks;

/// <summary>
/// 健康检查
/// </summary>
public class DefualtHealthcheck : BaseAppPack
{
    public override bool IsBeforeUseRouting { get; }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthChecks();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHealthChecks("/healthz");
    }
}

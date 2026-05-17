using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Modules;

/// <summary>
/// 模拟框架内置模块（如 DefaultHealthCheckModule）
/// </summary>
public class TestHealthCheckModule : AppModule
{
    public override string Name => "内置健康检查模块";

    public bool ConfigureServicesCalled { get; private set; }
    public bool ConfigureCalled { get; private set; }

    public override void ConfigureServices(IServiceCollection services)
    {
        ConfigureServicesCalled = true;
        services.AddHealthChecks();
    }

    public override void Configure(IApplicationBuilder app)
    {
        ConfigureCalled = true;
    }
}

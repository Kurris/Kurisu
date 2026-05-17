using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Modules;

/// <summary>
/// 模拟用户重写框架内置模块
/// </summary>
public class CustomHealthCheckModule : TestHealthCheckModule
{
    public override string Name => "自定义健康检查模块";

    public new bool ConfigureServicesCalled { get; private set; }
    public new bool ConfigureCalled { get; private set; }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        ConfigureServicesCalled = true;
    }

    public override void Configure(IApplicationBuilder app)
    {
        base.Configure(app);
        ConfigureCalled = true;
    }
}

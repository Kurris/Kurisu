using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 程序模块
/// </summary>
/// <remarks>
/// 执行顺序ConfigureServices --> Invoke --> Configure
/// </remarks>
public abstract class AppModule : IAppModule
{
    public abstract string Name { get; }

    /// <summary>
    /// appsettings.json配置或者外部动态配置
    /// </summary>
    public IConfiguration Configuration { get; set; }

    /// <summary>
    /// 启动顺序
    /// </summary>
    public virtual int Order => 100;

    /// <summary>
    /// 是否启用
    /// </summary>
    public virtual bool IsEnable => true;

    /// <summary>
    /// 在Startup Configure请求管道中,以UseRouting分割添加自定义模块, true表示在UseRouting之前添加, false表示在UseRouting之后添加
    /// </summary>
    public virtual bool IsBeforeUseRouting { get; } = false;

    /// <summary>
    /// 执行特定操作
    /// </summary>
    /// <param name="serviceProvider"></param>
    public virtual void Invoke(IServiceProvider serviceProvider)
    {
    }

    /// <summary>
    /// 配置模块功能所需的服务
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// 配置管道中间件
    /// </summary>
    /// <param name="app"></param>
    public virtual void Configure(IApplicationBuilder app)
    {
    }
}
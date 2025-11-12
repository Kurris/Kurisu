using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 程序模块
/// </summary>
public abstract class AppModule : IAppModule
{
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
    /// 在Startup Configure请求管道中,以UseRouting分割添加自定义模块
    /// </summary>
    public virtual bool IsBeforeUseRouting { get; } = false;

    /// <summary>
    /// 2.执行
    /// </summary>
    /// <param name="serviceProvider"></param>
    public virtual void Invoke(IServiceProvider serviceProvider)
    {
    }

    /// <summary>
    /// 1.配置ioc
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// 3.配置管道
    /// </summary>
    /// <param name="app"></param>
    public virtual void Configure(IApplicationBuilder app)
    {
    }
}
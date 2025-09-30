using System;
using Kurisu.AspNetCore.Startup.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup;

/// <summary>
/// 程序包
/// </summary>
public abstract class BaseAppPack : IAppPack
{
    /// <summary>
    /// appsetting.json配置或者外部动态配置
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
    /// 在Startup Configure请求管道中,以UseRouting分割添加自定义AppPack
    /// </summary>
    public virtual bool IsBeforeUseRouting { get; }
    
    /// <summary>
    /// 执行2
    /// </summary>
    /// <param name="serviceProvider"></param>
    public virtual void Invoke(IServiceProvider serviceProvider)
    {
    }

    /// <summary>
    /// 配置ioc1
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
      
    }

    /// <summary>
    /// 配置管道3
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
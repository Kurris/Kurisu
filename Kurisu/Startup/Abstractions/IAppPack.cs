using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.Abstractions;

/// <summary>
/// 定义appPack行为,模拟标准Startup
/// </summary>
public interface IAppPack : IOrderedFilter
{
    /// <summary>
    /// 触发方法
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    // ReSharper disable once UnusedParameter.Global
    void Invoke(IServiceProvider serviceProvider);

    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="services">服务容器</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// 管道服务配置
    /// </summary>
    /// <param name="app">app管道</param>
    /// <param name="env">环境</param>
    void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}
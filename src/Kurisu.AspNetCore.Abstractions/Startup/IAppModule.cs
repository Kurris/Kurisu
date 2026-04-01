using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 定义appPack行为,模拟标准Startup
/// </summary>
public interface IAppModule : IOrderedFilter
{
    /// <summary>
    /// 触发方法
    /// </summary>
    /// <param name="serviceProvider">临时scope服务提供器</param>
    void Invoke(IServiceProvider serviceProvider);

    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="services"></param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// 管道服务配置
    /// </summary>
    /// <param name="app">app管道</param>
    void Configure(IApplicationBuilder app);
}
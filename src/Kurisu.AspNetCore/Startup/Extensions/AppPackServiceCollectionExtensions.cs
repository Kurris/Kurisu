using System;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.Extensions;

/// <summary>
/// 程序自定义包扩展
/// </summary>
[SkipScan]
public static class AppPackServiceCollectionExtensions
{
    /// <summary>
    /// 添加自定义appPacks
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddAppModules(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var appPack in App.Modules.Value)
        {
            appPack.Configuration = configuration;
            if (appPack.IsEnable)
            {
                appPack.ConfigureServices(services);
            }
        }

        return services;
    }

    /// <summary>
    /// 使用自定义appPacks
    /// </summary>
    /// <param name="app">应用程序</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="isBeforeUseRouting">在使用UseRouting之前</param>
    /// <returns></returns>
    public static IApplicationBuilder UseAppPacks(this IApplicationBuilder app, IServiceProvider serviceProvider, bool isBeforeUseRouting)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var modules = App.Modules.Value.Where(x => x.IsBeforeUseRouting == isBeforeUseRouting);

        foreach (var module in modules)
        {
            module.Configuration = configuration;
            if (!module.IsEnable)
            {
                continue;
            }

            module.Invoke(serviceProvider);
            module.Configure(app);
        }

        return app;
    }
}
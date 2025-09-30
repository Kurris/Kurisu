using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
    public static IServiceCollection AddAppPacks(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var appPack in App.AppPacks)
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
    /// <param name="env">web环境</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="isBeforeUseRouting">在使用UseRouting之前</param>
    /// <returns></returns>
    public static IApplicationBuilder UseAppPacks(this IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, bool isBeforeUseRouting)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        foreach (var appPack in App.AppPacks.Where(x => x.IsBeforeUseRouting == isBeforeUseRouting))
        {
            appPack.Configuration = configuration;
            if (!appPack.IsEnable)
            {
                continue;
            }

            appPack.Invoke(serviceProvider);
            appPack.Configure(app, env);
        }

        return app;
    }
}
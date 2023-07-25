using System;
using System.Linq;
using Kurisu;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddKurisuAppPacks(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var appPack in App.AppPacks)
        {
            appPack.Configuration = configuration;
            appPack.ConfigureServices(services);
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
    public static IApplicationBuilder UseKurisuAppPacks(this IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, bool isBeforeUseRouting)
    {
        var configuration = serviceProvider.GetService<IConfiguration>();
        foreach (var appPack in App.AppPacks.Where(x => x.IsBeforeUseRouting == isBeforeUseRouting))
        {
            appPack.Configuration = configuration;
            appPack.Invoke(serviceProvider);
            appPack.Configure(app, env);
        }

        return app;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Startup;
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

        ValidateModuleOrder(App.Modules.Value);
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

    /// <summary>
    /// 检测启用模块的 Order 配置是否合法
    /// </summary>
    private static void ValidateModuleOrder(List<AppModule> modules)
    {
        var enabledModules = modules
            .Where(m => m.IsEnable)
            .ToList();

        var negativeOrders = enabledModules
            .Where(m => m.Order < 0)
            .Select(m => $"{m.GetType().Name}(Order={m.Order})")
            .ToList();

        if (negativeOrders.Count > 0)
        {
            throw new InvalidOperationException(
                $"检测到非法模块顺序，Order 不能小于 0。冲突详情：{string.Join("; ", negativeOrders)}");
        }

        var conflicts = enabledModules
            .GroupBy(m => new { m.IsBeforeUseRouting, m.Order })
            .Where(g => g.Count() > 1)
            .Select(g =>
                $"{(g.Key.IsBeforeUseRouting ? "BeforeUseRouting" : "AfterUseRouting")}, Order={g.Key.Order}: {string.Join(", ", g.Select(m => m.GetType().Name))}")
            .ToList();

        if (conflicts.Count > 0)
        {
            throw new InvalidOperationException(
                $"检测到同类型模块冲突，相同 Order 的模块只允许开启一个。冲突详情：{string.Join("; ", conflicts)}");
        }
    }
}

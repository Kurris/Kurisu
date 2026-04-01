using System;
using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.Startup;

namespace Kurisu.AspNetCore;

/// <summary>
/// 应用程序全局类
/// </summary>
public class App
{
    /// <summary>
    /// 启动项配置
    /// </summary>
    public static StartupOptions StartupOptions { get; } = new();

    /// <summary>
    /// 自定义应用pack
    /// </summary>
    internal static Lazy<List<AppModule>> Modules = new(() =>
    {
        var packTypes = DependencyInjectionHelper.ActiveTypes.Value
               .Where(x => typeof(AppModule).IsAssignableFrom(x)
                           && x != typeof(AppModule)
                           && !x.IsAbstract
               ).ToList();

        var leafTypes = packTypes
            .Where(x => !packTypes.Any(y => y != x && x.IsAssignableFrom(y)))
            .ToList();

        return leafTypes.Select(x => (AppModule)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Order)
            .ToList();
    });
}
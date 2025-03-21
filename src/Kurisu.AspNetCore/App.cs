using System;
using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.AspNetCore;

/// <summary>
/// 应用程序全局类
/// </summary>
public class App
{
    /// <summary>
    /// 启动项配置
    /// </summary>
    public StartupOptions Options { get; set; }

    /// <summary>
    /// 框架应用程序日志
    /// </summary>
    public static ILogger<App> Logger => InternalApp.RootServices.GetService<ILogger<App>>();

    /// <summary>
    /// 服务提供器
    /// </summary>
    /// <returns></returns>
    public static IServiceProvider GetServiceProvider(bool fromRoot = false)
    {
        if (fromRoot)
        {
            return InternalApp.RootServices;
        }

        var httpContext = InternalApp.RootServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        return httpContext.RequestServices;
    }

    /// <summary>
    /// 自定义应用pack
    /// </summary>
    private static List<BaseAppPack> _appPacks;

    /// <summary>
    /// 自定义应用pack
    /// </summary>
    internal static List<BaseAppPack> AppPacks
    {
        get
        {
            if (_appPacks != null) return _appPacks;

            var packTypes = DependencyInjectionHelper.ActiveTypes.Where(x => x.IsSubclassOf(typeof(BaseAppPack)));
            _appPacks = packTypes.Select(x => Activator.CreateInstance(x) as BaseAppPack).Where(x => x != null).OrderBy(x => x.Order).ToList();
            return _appPacks;
        }
    }

    /// <summary>
    /// 可用类
    /// </summary>
    public static List<Type> ActiveTypes => DependencyInjectionHelper.ActiveTypes;

    /// <summary>
    /// 可作为依赖注入的服务类
    /// </summary>
    public static IEnumerable<Type> DependencyServices => DependencyInjectionHelper.DependencyServices;
}
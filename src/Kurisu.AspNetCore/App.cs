using System;
using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
    /// 框架应用程序日志
    /// </summary>
    public static ILogger Logger { get; } = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Prefix} [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Debug()
                .CreateLogger()
            );
        })
        .CreateLogger("Kurisu.AspNetCore.App");

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
    /// 根服务
    /// </summary>
    public static IServiceProvider RootServices => GetServiceProvider(true);

    /// <summary>
    /// 请求的服务
    /// </summary>
    public static IServiceProvider RequestServices => GetServiceProvider();

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

            var packTypes = DependencyInjectionHelper.ActiveTypes.Value.Where(x => x.IsSubclassOf(typeof(BaseAppPack)));
            _appPacks = packTypes.Select(x => (BaseAppPack)Activator.CreateInstance(x)!).OrderBy(x => x.Order).ToList();
            return _appPacks;
        }
    }

    /// <summary>
    /// 可用类
    /// </summary>
    public static List<Type> ActiveTypes => DependencyInjectionHelper.ActiveTypes.Value;

    /// <summary>
    /// 可作为依赖注入的服务类
    /// </summary>
    public static IEnumerable<Type> DependencyServices => DependencyInjectionHelper.DependencyServices.Value;
}
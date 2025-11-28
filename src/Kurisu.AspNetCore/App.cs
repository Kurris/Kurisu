using System;
using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
    public static ILogger Logger
    {
        get
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            var configurationRoot = builder.Build();

            return LoggerFactory.Create(config =>
                {
                    config.AddSerilog(new LoggerConfiguration()
                        .ReadFrom.Configuration(configurationRoot)
                        .CreateLogger()
                    );
                })
                .CreateLogger("App");
        }
    }

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
    private static List<AppModule> _appModules;

    /// <summary>
    /// 自定义应用pack
    /// </summary>
    internal static List<AppModule> AppModules
    {
        get
        {
            if (_appModules != null) return _appModules;

            var packTypes = DependencyInjectionHelper.ActiveTypes.Value
                .Where(x => typeof(AppModule).IsAssignableFrom(x)
                            && x != typeof(AppModule)
                            && !x.IsAbstract
                ).ToList();

            var leafTypes = packTypes
                .Where(x => !packTypes.Any(y => y != x && x.IsAssignableFrom(y)))
                .ToList();

            _appModules = leafTypes.Select(x => (AppModule)Activator.CreateInstance(x)!)
                .OrderBy(x => x.Order)
                .ToList();

            return _appModules;
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
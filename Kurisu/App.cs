using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu;

/// <summary>
/// 应用程序全局类
/// </summary>
public class App
{
    static App()
    {
        Initialize();
    }


    /// <summary>
    /// 框架应用程序启动日志
    /// </summary>
    internal static ILogger<App> Logger => InternalApp.ApplicationServices.GetService<ILogger<App>>();

    /// <summary>
    /// 请求上下文
    /// </summary>
    public static HttpContext HttpContext => InternalApp.ApplicationServices.GetService<IHttpContextAccessor>()?.HttpContext;

    /// <summary>
    /// 服务提供器
    /// </summary>
    /// <remarks>
    /// 默认从请求作用域中获取,可指定从根服务中创建新的作用域(Root ServiceProvider 创建的域或者服务都需要手动释放),该作用域会在请求结束后释放
    /// </remarks>
    /// <returns></returns>
    internal static IServiceProvider GetServiceProvider(bool fromRootService = false)
    {
        var httpContext = HttpContext;

        if (!fromRootService)
        {
            if (httpContext?.RequestServices != null)
                return httpContext.RequestServices;
        }

        //根服务创建的作用域,需要自定义管理
        var scope = InternalApp.ApplicationServices.CreateAsyncScope();
        AddDisposableObject(scope);
        return scope.ServiceProvider;
    }


    /// <summary>
    /// 可释放的对象
    /// </summary>
    private static readonly ConcurrentBag<IDisposable> Disposables = new();

    /// <summary>
    /// 添加可释放的对象
    /// </summary>
    /// <param name="disposable"></param>
    public static void AddDisposableObject(IDisposable disposable)
    {
        Disposables.Add(disposable);
    }

    /// <summary>
    /// 释放对象
    /// </summary>
    public static void DisposeObjects()
    {
        foreach (var disposable in Disposables)
        {
            disposable.Dispose();
        }

        Disposables.Clear();
    }


    /// <summary>
    /// 应用程序有效类型
    /// </summary>
    public static List<Type> ActiveTypes { get; private set; }

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

            var packTypes = ActiveTypes.Where(x => x.IsSubclassOf(typeof(BaseAppPack)));
            _appPacks = packTypes.Select(x => Activator.CreateInstance(x) as BaseAppPack)
                .Where(x => x.IsEnable)
                .OrderBy(x => x.Order)
                .ToList();

            return _appPacks;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private static void Initialize()
    {
        LoadActiveTypes();
    }

    /// <summary>
    /// 加载可用类型
    /// </summary>
    private static void LoadActiveTypes()
    {
        //所有程序集
        var activeAssemblies = new List<Assembly>();

        //添加当前程序集
        activeAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());
        var name = AppDomain.CurrentDomain.FriendlyName;

        var references = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
        //添加引用的程序集
        foreach (var reference in references)
        {
            if (activeAssemblies.Exists(x => x.FullName!.Equals(reference.FullName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var refAssembly = Assembly.Load(reference);
            activeAssemblies.Add(refAssembly);
        }

        ActiveTypes = activeAssemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetTypes()
                .Where(type => type.IsPublic)
                .Where(type => !type.IsDefined(typeof(SkipScanAttribute)));
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
        })
        .Reverse().ToList();

    }
}
using System;
using System.Collections.Concurrent;
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
    /// 框架应用程序日志
    /// </summary>
    internal static ILogger<App> Logger => InternalApp.RootServices.GetService<ILogger<App>>();

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

        var httpContext = InternalApp.RootServices.GetService<IHttpContextAccessor>().HttpContext;
        return httpContext.RequestServices;
    }


    /// <summary>
    /// 可释放的对象
    /// </summary>
    private static readonly ConcurrentBag<IDisposable> _disposables = new();

    /// <summary>
    /// 添加可释放的对象
    /// </summary>
    /// <param name="disposable"></param>
    private static void AddDisposableObject(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    /// <summary>
    /// 释放对象
    /// </summary>
    public static void DisposeObjects()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
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
            _appPacks = packTypes.Select(x => Activator.CreateInstance(x) as BaseAppPack).OrderBy(x => x.Order).ToList();
            return _appPacks;
        }
    }

    public static List<Type> ActiveTyps => DependencyInjectionHelper.ActiveTypes;
}
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
    public static readonly StartupOptions StartupOptions { get; } = new();

    /// <summary>
    /// 自定义应用pack
    /// </summary>
    public static readonly Lazy<List<AppModule>> Modules = new(() =>
    {
        var moduleTypes = DependencyInjectionHelper.ActiveTypes.Value
            .Where(x => x.IsClass && !x.IsAbstract && typeof(AppModule).IsAssignableFrom(x))
            .ToArray();

        // 收集所有作为其他模块基类的类型，O(n * 继承深度)
        var baseTypes = new HashSet<Type>();
        foreach (var t in moduleTypes)
        {
            for (var bt = t.BaseType; bt != null && bt != typeof(AppModule); bt = bt.BaseType)
            {
                baseTypes.Add(bt);
            }
        }

        return moduleTypes
            .Where(x => !baseTypes.Contains(x))
            .Select(x => (AppModule)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Order)
            .ToList();
    });
}
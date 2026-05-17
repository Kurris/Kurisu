using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Kurisu.AspNetCore.DependencyInjection;

internal static class DependencyInjectionHelper
{
    /// <summary>
    /// 应用程序有效类型
    /// </summary>
    public static readonly Lazy<List<Type>> ActiveTypes = new(LoadActiveTypes);

    /// <summary>
    /// 依赖注入类
    /// </summary>
    public static readonly Lazy<List<Type>> DependencyServices = new(() => ActiveTypes.Value
        .Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false, IsInterface: false })
        .Where(x => x.IsDefined(typeof(DiInjectAttribute), false))
        .ToList()
    );

    /// <summary>
    /// 配置类
    /// </summary>
    public static readonly Lazy<List<Type>> Configurations = new(() => ActiveTypes.Value
        .Where(x => x.IsDefined(typeof(ConfigurationAttribute)))
        .ToList()
    );


    public static (string named, ServiceLifetime lifeTime, Type[] serviceTypes) GetInjectInfos(Type type)
    {
        var interfaces = type.GetInterfaces();

        var abstractBaseTypes = new List<Type>();
        var baseType = type.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType.IsAbstract)
                abstractBaseTypes.Add(baseType);

            baseType = baseType.BaseType;
        }

        var info = type.GetCustomAttribute<DiInjectAttribute>()!;

        var combined = interfaces.Concat(abstractBaseTypes).Distinct();

        if (info.IgnoreServiceTypes is { Length: > 0 })
        {
            combined = combined.Except(info.IgnoreServiceTypes);
        }

        return (info.Named, info.Lifetime, combined.Where(x => x.IsVisible).ToArray());
    }

    /// <summary>
    /// 注册IOC
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime"></param>
    /// <param name="implementType"></param>
    /// <param name="serviceType"></param>
    /// <param name="named"></param>
    public static void Register(IServiceCollection services, ServiceLifetime lifetime, Type implementType, Type serviceType = null, string named = null)
    {
        implementType = GetGenericRealType(implementType);

        if (serviceType == null)
        {
            services.Add(ServiceDescriptor.Describe(implementType, implementType, lifetime));
        }
        else
        {
            serviceType = GetGenericRealType(serviceType);
            if (named is not null)
            {
                services.Add(ServiceDescriptor.DescribeKeyed(serviceType, named, implementType, lifetime));
            }
            else
            {
                services.Add(ServiceDescriptor.Describe(serviceType, implementType, lifetime));
            }
        }
    }

    /// <summary>
    /// 泛型类转换
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private static Type GetGenericRealType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsGenericType)
        {
            return type;
        }

        //非模板`1的接口
        if (type.GenericTypeArguments.Length > 0)
        {
            if (!type.GenericTypeArguments.First().IsGenericParameter)
            {
                return type;
            }
        }

        // FullName 对开放泛型（如 IGenericsGet<>）返回 null，回退到 Namespace.Name
        var interfaceFullName = type.FullName ?? $"{type.Namespace}.{type.Name}";
        type = type.Assembly.GetType(interfaceFullName);
        return type ?? throw new NullReferenceException(nameof(interfaceFullName));
    }

    /// <summary>
    /// 加载可用类型
    /// </summary>
    private static List<Type> LoadActiveTypes()
    {
        var assemblies = new HashSet<Assembly>();

        // 编译期完整依赖图（从 .deps.json 读取，只加载 project 类型的程序集）
        var context = DependencyContext.Default;
        if (context != null)
        {
            foreach (var lib in context.RuntimeLibraries)
            {
                if (!string.Equals(lib.Type, "project", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var name in lib.GetDefaultAssemblyNames(context))
                {
                    try { assemblies.Add(Assembly.Load(name)); }
                    catch { }
                }
            }
        }

        // 已加载的程序集兜底（含动态加载的 plugin 以及 DependencyContext 为 null 的情况）
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            assemblies.Add(asm);
        }

        return assemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetExportedTypes().Where(type => !type.IsDefined(typeof(SkipScanAttribute)));
            }
            catch (ReflectionTypeLoadException)
            {
                return [];
            }
        }).ToList();
    }
}
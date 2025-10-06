using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.AspNetCore.DependencyInjection;

internal static class DependencyInjectionHelper
{
    /// <summary>
    /// 命名服务
    /// </summary>
    internal static readonly ConcurrentDictionary<Tuple<Type, string>, Type> NamedServices = new();

    /// <summary>
    /// 应用程序有效类型
    /// </summary>
    public static readonly Lazy<List<Type>> ActiveTypes = new(LoadActiveTypes);

    /// <summary>
    /// 依赖注入类
    /// </summary>
    public static readonly Lazy<List<Type>> DependencyServices = new(() =>
    {
        return ActiveTypes.Value
            .Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false, IsInterface: false })
            .Where(x => x.IsDefined(typeof(DiInjectAttribute), false))
            .ToList();
    });


    /// <summary>
    /// 配置类
    /// </summary>
    public static readonly Lazy<List<Type>> Configurations = new(() => { return ActiveTypes.Value.Where(x => x.IsDefined(typeof(ConfigurationAttribute))).ToList(); });


    public static (string named, ServiceLifetime lifeTime, Type[] interfaceTypes) GetInjectInfos(Type type)
    {
        var interfaces = type.GetInterfaces();
        var info = type.GetCustomAttribute<DiInjectAttribute>()!;
        return (info.Named, info.Lifetime, interfaces);
    }

    /// <summary>
    /// 注册IOC
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime"></param>
    /// <param name="service"></param>
    /// <param name="interfaceType"></param>
    public static void Register(IServiceCollection services, ServiceLifetime lifetime, Type service, Type interfaceType = null)
    {
        //范型类型转换
        service = GetGenericRealType(service);


        ServiceDescriptor descriptor;
        if (interfaceType == null)
        {
            descriptor = ServiceDescriptor.Describe(service, service, lifetime);
        }
        else
        {
            interfaceType = GetGenericRealType(interfaceType);
            descriptor = ServiceDescriptor.Describe(interfaceType, service, lifetime);
        }

        services.Add(descriptor);

        App.Logger.LogDebug("注册{Lifetime}服务: {ServiceName} {ServiceInterface}", lifetime, service.Name, descriptor.ServiceType.Name);
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

        var interfaceFullName = type.Namespace + "." + type.Name;
        type = type.Assembly.GetType(interfaceFullName);
        return type ?? throw new NullReferenceException(nameof(interfaceFullName));
    }

    /// <summary>
    /// 加载可用类型
    /// </summary>
    private static List<Type> LoadActiveTypes()
    {
        // 所有程序集去重，使用FullName+Location唯一标识
        var activeAssemblies = new List<Assembly>();
        var assemblyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var key = asm.FullName + "|" + (asm.IsDynamic ? "dynamic" : asm.Location);
            if (assemblyKeys.Add(key))
                activeAssemblies.Add(asm);
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var references = entryAssembly.GetReferencedAssemblies();
            RecursionGetReference(activeAssemblies, assemblyKeys, references);
        }

        var result = activeAssemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetTypes().Where(type => type.IsPublic).Where(type => !type.IsDefined(typeof(SkipScanAttribute)));
            }
            catch
            {
                return [];
            }
        }).ToList();

        return result;
    }

    // 优化后的递归收集引用程序集方法，结构更清晰，去重逻辑更优雅
    private static void RecursionGetReference(List<Assembly> activeAssemblies, HashSet<string> assemblyKeys, IEnumerable<AssemblyName> references)
    {
        foreach (var reference in references)
        {
            try
            {
                var refAssembly = Assembly.Load(reference);
                string location;
                try
                {
                    location = refAssembly.IsDynamic ? "dynamic" : refAssembly.Location;
                }
                catch
                {
                    continue;
                }

                var key = refAssembly.FullName + "|" + location;
                if (!assemblyKeys.Add(key))
                    continue;
                activeAssemblies.Add(refAssembly);
                var next = refAssembly.GetReferencedAssemblies();
                if (next.Length > 0)
                    RecursionGetReference(activeAssemblies, assemblyKeys, next);
            }
            catch
            {
                //ignore
            }
        }
    }
}
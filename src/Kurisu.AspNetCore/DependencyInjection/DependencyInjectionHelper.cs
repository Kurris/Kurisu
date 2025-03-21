using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.DependencyInjection;

internal static class DependencyInjectionHelper
{
    private static List<Type> _activeTypes;

    /// <summary>
    /// 应用程序有效类型
    /// </summary>
    public static List<Type> ActiveTypes
    {
        get { return _activeTypes ??= LoadActiveTypes(); }
    }


    //生命周期类型
    private static readonly Type[] _lifeTimeTypes = { typeof(ITransientDependency), typeof(IScopeDependency), typeof(ISingletonDependency) };

    public static readonly IEnumerable<Type> Services = ActiveTypes.Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false, IsInterface: false });

    public static readonly IEnumerable<Type> DependencyServices = ActiveTypes.Where(x => !x.IsAbstract).Where(x => x.IsAssignableTo(typeof(IDependency)));

    /// <summary>
    /// 命名服务
    /// </summary>
    public static readonly ConcurrentDictionary<string, Type> NamedServices = new();

    /// <summary>
    /// 获取类的接口和IOC生命周期
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static (ServiceLifetime lifeTime, Type[] interfaceTypes) GetInterfacesAndLifeTime(Type type)
    {
        //获取服务的所有接口
        var interfaces = type.GetInterfaces();

        //获取注册的生命周期类型,不允许多个依赖注入接口
        var lifeTimeInterfaces = interfaces.Where(x => _lifeTimeTypes.Contains(x)).Distinct().ToArray();
        if (lifeTimeInterfaces.Length > 1) throw new ApplicationException($"{type.FullName}不允许存在多个生命周期定义");

        //具体的生命周期类型
        var currentLifeTimeInterface = lifeTimeInterfaces.FirstOrDefault();

        //能够注册的服务接口
        var serviceInterfaces = interfaces
            .Where(x => !x.IsAssignableFrom(typeof(IDependency)))
            .Where(x => !_lifeTimeTypes.Contains(x));

        return (GetRegisterLifetimeType(currentLifeTimeInterface), serviceInterfaces.ToArray());
    }


    /// <summary>
    /// 判断生命周期
    /// </summary>
    /// <param name="lifeTimeType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    private static ServiceLifetime GetRegisterLifetimeType(MemberInfo lifeTimeType)
    {
        if (lifeTimeType == null) return ServiceLifetime.Scoped;

        //判断注册方式
        return lifeTimeType.Name switch
        {
            nameof(ITransientDependency) => ServiceLifetime.Transient,
            nameof(ISingletonDependency) => ServiceLifetime.Singleton,
            nameof(IScopeDependency) => ServiceLifetime.Scoped,
            _ => ServiceLifetime.Scoped
        };
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

        if (interfaceType == null)
            services.Add(ServiceDescriptor.Describe(service, service, lifetime));
        else
        {
            interfaceType = GetGenericRealType(interfaceType);
            services.Add(ServiceDescriptor.Describe(interfaceType, service, lifetime));
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
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        if (type == null)
            throw new NullReferenceException(nameof(interfaceFullName));

        return type;
    }

    /// <summary>
    /// 加载可用类型
    /// </summary>
    private static List<Type> LoadActiveTypes()
    {
        //所有程序集
        var activeAssemblies = new List<Assembly>();

        //添加当前程序集
        activeAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

        var references = Assembly.GetEntryAssembly()?.GetReferencedAssemblies();
        //添加引用的程序集
        RecursionGetReference(activeAssemblies, references);

        var result = activeAssemblies.SelectMany(assembly =>
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

        return result;
    }

    private static void RecursionGetReference(List<Assembly> activeAssemblies, IEnumerable<AssemblyName> references)
    {
        foreach (var reference in references)
        {
            if (activeAssemblies.Exists(x => x.FullName!.Equals(reference.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var refAssembly = Assembly.Load(reference);
            var next = refAssembly.GetReferencedAssemblies();
            if (next.Any() && next.Any(x => x.Name!.StartsWith("Kurisu")))
            {
                RecursionGetReference(activeAssemblies, next);
            }

            activeAssemblies.Add(refAssembly);
        }
    }
}
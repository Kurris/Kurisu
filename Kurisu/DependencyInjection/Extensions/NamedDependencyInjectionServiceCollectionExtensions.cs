using System;
using Kurisu.DependencyInjection.Internal;
using Kurisu.DependencyInjection;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 命名服务处理扩展
/// </summary>
[SkipScan]
internal static class NamedDependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// 注册命名服务
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static void RegisterNamedServices(this IServiceCollection services)
    {
        services.AddNamedResolver();

        var serviceTypes = DependencyInjectionHelper.Services.Where(x => !x.IsAbstract).Where(x => x.IsDefined(typeof(ServiceAttribute), false));
        foreach (var service in serviceTypes)
        {
            if (service.IsGenericType)
            {
                throw new NotSupportedException($"不支持泛型类{service.FullName}生成命名服务");
            }

            var serviceAttribute = service.GetCustomAttribute<ServiceAttribute>();
            var typeNamed = serviceAttribute.Named;

            DependencyInjectionHelper.NamedServices.TryAdd(typeNamed, service);
            //具体的生命周期类型
            var lifetime = DependencyInjectionHelper.GetRegisterLifetimeType(serviceAttribute.LifeTime);

            DependencyInjectionHelper.Register(services, lifetime, service);
        }
    }


    /// <summary>
    /// 命名服务扩展类
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    internal static IServiceCollection AddNamedResolver(this IServiceCollection services)
    {
        services.AddScoped<INamedResolver, NamedResolver>();
        return services;
    }
}
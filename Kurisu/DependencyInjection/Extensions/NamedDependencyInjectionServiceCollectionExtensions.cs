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
        var serviceTypes = DependencyInjectionHelper.Services.Where(x => x.IsDefined(typeof(ServiceAttribute), false));
        if (!serviceTypes.Any())
        {
            return;
        }
        services.AddNamedResolver();

        foreach (var service in serviceTypes)
        {
            //获取服务的所有接口
            var interfaces = service.GetInterfaces();
            var serviceAttribute = service.GetCustomAttribute<ServiceAttribute>();
            var typeNamed = serviceAttribute.Named;
            //具体的生命周期类型
            var currentLifeTimeInterface = serviceAttribute.LifeTime;

            if (service.IsGenericType)
            {
                throw new NotSupportedException($"不支持泛型类{service.FullName}生成命名服务");
            }

            var (lifeTime, interfaceTypes) = DependencyInjectionHelper.GetInterfacesAndLifeTime(service);

            //注册服务
            if (interfaceTypes.Any())
            {
                //注册所有接口
                foreach (var interfaceType in interfaceTypes)
                    DependencyInjectionHelper.Register(services, lifeTime, service, interfaceType);
            }
            else
            {
                DependencyInjectionHelper.Register(services, lifeTime, service);
            }
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
using System;
using System.Linq;
using Kurisu.AspNetCore.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 依赖注入扩展类
/// </summary>
[SkipScan]
public static class DependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// 自动依赖注入
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        services.RegisterServices();
        services.RegisterNamedServices();
        return services;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ApplicationException"></exception>
    private static void RegisterServices(this IServiceCollection services)
    {
        var serviceTypes = DependencyInjectionHelper.DependencyServices;

        foreach (var service in serviceTypes)
        {
            (ServiceLifetime lifeTime, Type[] interfaceTypes) = DependencyInjectionHelper.GetInterfacesAndLifeTime(service);

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
}
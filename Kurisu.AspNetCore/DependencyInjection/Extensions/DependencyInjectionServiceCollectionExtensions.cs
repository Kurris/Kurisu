using System;
using System.Linq;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 依赖注入扩展类
/// </summary>
[SkipScan]
public static class DependencyInjectionServiceCollectionExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        services.RegisterServices();
        services.RegisterNamedServices();
        services.RegisterInterceptorServices();
        return services;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ApplicationException"></exception>
    private static void RegisterServices(this IServiceCollection services)
    {
        var serviceTypes = DependencyInjectionHelper.Services.Where(x => !x.IsAbstract).Where(x => x.IsAssignableTo(typeof(IDependency)));

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
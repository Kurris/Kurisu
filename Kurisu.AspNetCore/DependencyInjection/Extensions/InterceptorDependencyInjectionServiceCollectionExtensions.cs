using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.Core.Proxy;
using Kurisu.Core.Proxy.Abstractions;
using Kurisu.Core.Proxy.Attributes;
using Kurisu.Core.Proxy.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

internal static class InterceptorDependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// 注册拦截器
    /// </summary>
    /// <param name="services"></param>
    internal static void RegisterInterceptorServices(this IServiceCollection services)
    {
        services.RegisterInterceptors();

        var serviceTypes = DependencyInjectionHelper.Services
            .Where(x => x.IsAssignableTo(typeof(IDependency)))
            .Where(x => !x.IsAbstract && !x.IsInterface)
            .Where(x => !x.IsDefined(typeof(ServiceAttribute), false));

        foreach (var service in serviceTypes)
        {
            (ServiceLifetime lifeTime, Type[] interfaceTypes) = DependencyInjectionHelper.GetInterfacesAndLifeTime(service);
            var interceptorTypes = ProxyMap.GetAllInterceptorTypes(service, interfaceTypes);

            if (!interceptorTypes.Any()) continue;

            foreach (var interfaceType in interfaceTypes)
            {
                services.Add(ServiceDescriptor.Describe(interfaceType, sp =>
                {
                    var target = sp.GetService(service);
                    var result = target;

                    foreach (var interceptorType in interceptorTypes)
                    {
                        var i = sp.GetService(interceptorType);
                        var interceptorObject = i switch
                        {
                            IAsyncInterceptor asyncInterceptor => asyncInterceptor.ToInterceptor(),
                            IInterceptor interceptor => interceptor,
                            _ => throw new NotSupportedException(interceptorType.FullName)
                        };

                        result = ProxyGenerator.Create(result, interfaceType, interceptorObject);
                    }

                    return result;
                }, lifeTime));
            }

            DependencyInjectionHelper.Register(services, lifeTime, service);
        }


        services.RegisterInterfaceInterceptorServices();
    }

    internal static void RegisterInterfaceInterceptorServices(this IServiceCollection services)
    {
        var interfaceTypes = DependencyInjectionHelper.ActiveTypes.Where(x => x.IsInterface && x.IsDefined(typeof(AopAttribute), false)).ToList();
        interfaceTypes.ForEach(x => { x.GetCustomAttributes<AopAttribute>().ToList().ForEach(aop => aop.ConfigureServices(services)); });

        foreach (var interfaceType in interfaceTypes)
        {
            var interceptorTypes = ProxyMap.GetAllInterceptorTypes(null, new[] { interfaceType }).ToList();
            if (!interceptorTypes.Any()) continue;

            services.Add(ServiceDescriptor.Describe(interfaceType, sp =>
            {
                object result = null;
                foreach (var interceptorType in interceptorTypes)
                {
                    var i = sp.GetService(interceptorType);
                    var interceptorObject = i switch
                    {
                        IAsyncInterceptor asyncInterceptor => asyncInterceptor.ToInterceptor(),
                        IInterceptor interceptor => interceptor,
                        _ => throw new NotSupportedException(interceptorType.FullName)
                    };

                    result = ProxyGenerator.Create(result, interfaceType, interceptorObject);
                }

                return result;
            }, ServiceLifetime.Singleton));
        }
    }

    /// <summary>
    /// 注册AOP实现类型
    /// </summary>
    /// <param name="services"></param>
    private static void RegisterInterceptors(this IServiceCollection services)
    {
        var interceptors = DependencyInjectionHelper.Services.Where(x => x.IsAssignableTo(typeof(Aop)) || x.IsAssignableTo(typeof(Aop)));
        foreach (var interceptor in interceptors)
        {
            DependencyInjectionHelper.Register(services, ServiceLifetime.Singleton, interceptor);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.Proxy;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy.Attributes;
using Kurisu.Proxy.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.DependencyInjection.Extensions;


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
            .Where(x => !x.IsAbstract)
            .Where(x => !x.IsDefined(typeof(ServiceAttribute), false));

        foreach (var service in serviceTypes)
        {
            var (lifeTime, interfaceTypes) = DependencyInjectionHelper.GetInterfacesAndLifeTime(service);
            var interceptorTypes = service.GetAllInterceptorTypes(interfaceTypes);

            if (interceptorTypes.Any())
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    services.Replace(ServiceDescriptor.Describe(interfaceType, sp =>
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
        }

        var a = (Type service, Type interceptor, Type interfaceType) =>
        {
            Func<IServiceProvider, object> handler = sp =>
            {
                var target = sp.GetService(service);
                var i = sp.GetService(interceptor);
                var interceptorObject = i switch
                {
                    IAsyncInterceptor asyncInterceptor => asyncInterceptor.ToInterceptor(),
                    IInterceptor interceptor => interceptor,
                    _ => throw new NotSupportedException(interceptor.FullName)
                };

                return ProxyGenerator.Create(target, interfaceType, interceptorObject);
            };

            return handler;
        };
    }


    internal static IEnumerable<Type> GetInterceptorTypes(this Type type)
    {
        var attributes = type.GetCustomAttributes<AopAttribute>();
        return attributes.SelectMany(x => x.Interceptors);
    }

    internal static IEnumerable<Type> GetInterceptorTypes(this MethodInfo[] methods)
    {
        return methods.SelectMany(method => method.GetCustomAttribute<AopAttribute>()?.Interceptors ?? Array.Empty<Type>());
    }


    internal static IEnumerable<Type> GetAllInterceptorTypes(this Type service, IEnumerable<Type> interfaceTypes)
    {
        var interceptorTypes = service.GetInterceptorTypes().ToList();

        foreach (var item in service.GetMethods().GetInterceptorTypes().Distinct())
        {
            if (!interceptorTypes.Contains(item))
            {
                interceptorTypes.Add(item);
            }
        }

        foreach (var interfaceType in interfaceTypes)
        {
            foreach (var item in interfaceType.GetInterceptorTypes())
            {
                if (!interceptorTypes.Contains(item))
                {
                    interceptorTypes.Add(item);
                }
            }
            foreach (var item in interfaceType.GetMethods().GetInterceptorTypes().Distinct())
            {
                if (!interceptorTypes.Contains(item))
                {
                    interceptorTypes.Add(item);
                }
            }
        }

        return interceptorTypes;
    }

    internal static void RegisterInterceptors(this IServiceCollection services)
    {
        var interceptors = DependencyInjectionHelper.Services.Where(x => x.IsAssignableTo(typeof(IInterceptor)) || x.IsAssignableTo(typeof(IAsyncInterceptor)));
        foreach (var item in interceptors)
        {
            DependencyInjectionHelper.Register(services, ServiceLifetime.Singleton, item);
        }
    }
}
using System.Reflection;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Proxy.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 依赖注入
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 添加远程调用
    /// </summary>
    /// <param name="services"></param>
    /// <param name="activeTypes"></param>
    /// <returns></returns>
    public static IServiceCollection Inject(this IServiceCollection services, IEnumerable<Type> activeTypes)
    {
        services.AddSingleton<DefaultRemoteCallClient>();

        var interfaceTypes = activeTypes.Where(x => x.IsInterface && x.IsDefined(typeof(EnableRemoteClientAttribute), false)).ToList();
        foreach (var item in interfaceTypes)
        {
            item.GetCustomAttributes<EnableRemoteClientAttribute>().ToList().ForEach(aop => aop.ConfigureServices(services));
        }

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

        return services;
    }

    /// <summary>
    /// 添加远程调用
    /// </summary>
    /// <param name="services"></param>
    /// <param name="activeTypes"></param>
    /// <returns></returns>
    public static IServiceCollection AddRemoteCall(this IServiceCollection services, IEnumerable<Type> activeTypes)
    {
        return services.Inject(activeTypes);
    }
}
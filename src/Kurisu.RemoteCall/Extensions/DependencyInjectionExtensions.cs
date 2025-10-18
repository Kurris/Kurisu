using System.Reflection;
using Kurisu.RemoteCall;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Extensions;
using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public static IRemoteCallBuilder AddRemoteCall(this IServiceCollection services, IEnumerable<Type> activeTypes)
    {
        services.AddHttpClient();
        services.TryAddSingleton<IRemoteCallUrlResolver, DefaultRemoteCallUrlResolver>();
        services.TryAddSingleton<IRemoteCallParameterValidator, DefaultParameterValidator>();
        services.TryAddSingleton<IRemoteCallResultHandler, DefaultRemoteCallResultHandler>();
        services.AddSingleton<HttpClientRemoteCallClient>();

        var interfaceTypes = activeTypes.Where(x => x.IsInterface && x.IsDefined(typeof(EnableRemoteClientAttribute), false)).ToList();
        foreach (var rc in interfaceTypes.Select(item => item.GetCustomAttribute<EnableRemoteClientAttribute>()!))
        {
            rc.ConfigureServices(services);
        }

        foreach (var interfaceType in interfaceTypes)
        {
            services.Add(ServiceDescriptor.Describe(interfaceType, sp =>
            {
                var client = sp.GetRequiredService<HttpClientRemoteCallClient>();
                var interceptor = client.ToInterceptor();

                return ProxyGenerator.Create(sp, interfaceType, interceptor);
            }, ServiceLifetime.Singleton));
        }

        return new RemoteCallBuilder
        {
            Services = services
        };
    }
}
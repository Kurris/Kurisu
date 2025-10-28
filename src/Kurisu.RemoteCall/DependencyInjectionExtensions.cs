using System.Reflection;
using Kurisu.RemoteCall;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
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
    public static void AddRemoteCall(this IServiceCollection services, IEnumerable<Type> activeTypes)
    {
        services.AddRemoteCall(string.Empty, activeTypes);
    }


    /// <summary>
    /// 添加远程调用
    /// </summary>
    /// <param name="services"></param>
    /// <param name="defaultClientName"></param>
    /// <param name="activeTypes"></param>
    /// <returns></returns>
    public static void AddRemoteCall(this IServiceCollection services, string defaultClientName, IEnumerable<Type> activeTypes)
    {
        if (!string.IsNullOrEmpty(defaultClientName))
        {
            RemoteCallStatic.DefaultClientName = defaultClientName;
        }

        services.TryAddSingleton<IRemoteCallUrlResolver, DefaultRemoteCallUrlResolver>();
        services.TryAddSingleton<IRemoteCallParameterValidator, DefaultParameterValidator>();
        services.TryAddSingleton<DefaultRemoteCallResponseResultHandler>();
        services.TryAddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
        services.TryAddSingleton<ICommonUtils, CommonUtils>();
        services.AddSingleton<HttpClientRemoteCallClient>();

        var interfaceTypes = activeTypes.Where(x => x.IsInterface && x.IsDefined(typeof(EnableRemoteClientAttribute), false));
        foreach (var interfaceType in interfaceTypes)
        {
            var enableRemoteClientAttribute = interfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;
            enableRemoteClientAttribute.ConfigureServices(services);

            var scanCustomHandlers = ScanCustomHandlers(interfaceType);
            foreach (var type in scanCustomHandlers.Distinct())
            {
                services.TryAddSingleton(type);
            }

            services.Add(ServiceDescriptor.Describe(interfaceType, sp =>
            {
                var client = sp.GetRequiredService<HttpClientRemoteCallClient>();
                var interceptor = client.ToInterceptor();

                return ProxyGenerator.Create(sp, interfaceType, interceptor);
            }, ServiceLifetime.Singleton));
        }
    }

    private static IEnumerable<Type> ScanCustomHandlers(Type type)
    {
        var requestAuthorizeAttribute = type.GetCustomAttribute<RequestAuthorizeAttribute>();
        if (requestAuthorizeAttribute is { Handler: not null })
        {
            yield return requestAuthorizeAttribute.Handler;
        }

        var responseResultHandlerAttribute = type.GetCustomAttribute<ResponseResultAttribute>();
        if (responseResultHandlerAttribute != null)
        {
            yield return responseResultHandlerAttribute.Handler;
        }

        var requestContentHandlerAttribute = type.GetCustomAttribute<RequestContentAttribute>();
        if (requestContentHandlerAttribute != null)
        {
            yield return requestContentHandlerAttribute.Handler;
        }


        foreach (var methodInfo in type.GetMethods())
        {
            foreach (var scanCustomHandler in ScanCustomHandlers(methodInfo))
            {
                yield return scanCustomHandler;
            }
        }
    }

    private static IEnumerable<Type> ScanCustomHandlers(MemberInfo memberInfo)
    {
        var requestAuthorizeAttribute = memberInfo.GetCustomAttribute<RequestAuthorizeAttribute>();
        if (requestAuthorizeAttribute is not null)
        {
            yield return requestAuthorizeAttribute.Handler;
        }

        var responseResultHandlerAttribute = memberInfo.GetCustomAttribute<ResponseResultAttribute>();
        if (responseResultHandlerAttribute is not null)
        {
            yield return responseResultHandlerAttribute.Handler;
        }

        var requestContentHandlerAttribute = memberInfo.GetCustomAttribute<RequestContentAttribute>();
        if (requestContentHandlerAttribute is not null)
        {
            yield return requestContentHandlerAttribute.Handler;
        }

        var requestHeaderAttribute = memberInfo.GetCustomAttribute<RequestHeaderAttribute>();
        if (requestHeaderAttribute is { Handler: not null })
        {
            yield return requestHeaderAttribute.Handler;
        }
    }
}
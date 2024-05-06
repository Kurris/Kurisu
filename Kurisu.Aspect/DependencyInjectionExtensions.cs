using Kurisu.Aspect.Core.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Aspect;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 启用动态代理
    /// </summary>
    /// <param name="services"></param>
    public static void AddAspect(this IServiceCollection services)
    {
        services.AddSingleton<IAspectExecutorFactory, AspectExecutorFactory>();
        services.AddSingleton<AspectBuilderFactory>();
        services.AddSingleton(typeof(AspectCaching<,>));
        services.AddSingleton<InterceptorCollector>();
        services.AddSingleton<IProxyTypeGenerator, ProxyTypeGenerator>();
    }
}
using Kurisu.Aspect.Core.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Aspect;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 启用动态代理
    /// </summary>
    /// <param name="services"></param>
    public static void Inject(this IServiceCollection services)
    {
        services.TryAddSingleton<IAspectExecutorFactory, AspectExecutorFactory>();
        services.TryAddSingleton<AspectBuilderFactory>();
        services.TryAddSingleton(typeof(AspectCaching<,>));
        services.TryAddSingleton<InterceptorCollector>();
        services.TryAddSingleton<IProxyTypeGenerator, ProxyTypeGenerator>();
    }

    public static void ReplaceProxyService(this IServiceCollection services, IEnumerable<ReplaceProxyServiceItem> toReplaces)
    {
        services.Inject();
        
        foreach (var toReplace in toReplaces)
        {
            foreach (var @interface in toReplace.Service.GetInterfaces().Where(x => x.GetMethods().Length > 0))
            {
                services.Replace(new ServiceDescriptor(@interface, sp =>
                {
                    var generator = sp.GetRequiredService<IProxyTypeGenerator>();
                    var type = generator.CreateClassProxyType(toReplace.Service, toReplace.Service);
                    return ActivatorUtilities.CreateInstance(sp, type);
                    
                }, toReplace.Lifetime));
            }
        }
    }
}
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
        
        foreach (var item in toReplaces)
        {
            //注册服务
            if (item.InterfaceTypes.Length != 0)
            {
                foreach (var interfaceType in item.InterfaceTypes)
                {
                    if (!string.IsNullOrEmpty(item.Named))
                    {
                        services.Replace(new ServiceDescriptor(item.Service, sp =>
                        {
                            var generator = sp.GetRequiredService<IProxyTypeGenerator>();
                            var type = generator.CreateClassProxyType(item.Service, item.Service);
                            return ActivatorUtilities.CreateInstance(sp, type);
                    
                        }, item.Lifetime));
                    }
                    else
                    {
                        services.Replace(new ServiceDescriptor(interfaceType, sp =>
                        {
                            var generator = sp.GetRequiredService<IProxyTypeGenerator>();
                            var type = generator.CreateClassProxyType(item.Service, item.Service);
                            return ActivatorUtilities.CreateInstance(sp, type);
                    
                        }, item.Lifetime));
                    }
                }
            }
            else
            {
                services.Replace(new ServiceDescriptor(item.Service, sp =>
                {
                    var generator = sp.GetRequiredService<IProxyTypeGenerator>();
                    var type = generator.CreateClassProxyType(item.Service, item.Service);
                    return ActivatorUtilities.CreateInstance(sp, type);
                    
                }, item.Lifetime));
            }
        }
    }
}
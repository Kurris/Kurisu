using System.Linq;
using Kurisu.Aspect;
using Kurisu.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// aop拦截注入
/// </summary>
[SkipScan]
public static class InterceptorDependencyInjectionServiceCollection
{
    /// <summary>
    /// 添加Aop依赖注入
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddAop(this IServiceCollection services)
    {
        services.ReplaceProxyService(App.DependencyServices.Select(x =>
        {
            var di = x.GetInterfaces().Where(p => p.IsAssignableTo(typeof(IDependency))).FirstOrDefault(p => p != typeof(IDependency));

            var lifetime = di == typeof(ISingletonDependency) 
                ? ServiceLifetime.Singleton
                : di == typeof(IScopeDependency) 
                    ? ServiceLifetime.Scoped 
                    : ServiceLifetime.Transient;

            return new ReplaceProxyServiceItem
            {
                Lifetime = lifetime,
                Service = x
            };
        }));
        
        return services;
    }
}
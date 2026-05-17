using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 依赖注入扩展类
/// </summary>
[SkipScan]
public static class DependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// 自动依赖注入
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        services.RegisterServices();
        return services;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ApplicationException"></exception>
    private static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<INamedResolver, NamedResolver>();
        var implementTypes = DependencyInjectionHelper.DependencyServices.Value;

        foreach (var implementType in implementTypes)
        {
            var (named, lifeTime, serviceTypes) = DependencyInjectionHelper.GetInjectInfos(implementType);

            if (serviceTypes.Length != 0)
            {
                foreach (var serviceType in serviceTypes)
                {
                    DependencyInjectionHelper.Register(services, lifeTime, implementType, serviceType, named);
                }
            }
            else
            {
                DependencyInjectionHelper.Register(services, lifeTime, implementType);
            }
        }
    }
}
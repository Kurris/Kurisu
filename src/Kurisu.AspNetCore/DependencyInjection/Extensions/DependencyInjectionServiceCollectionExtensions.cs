using System;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection.Internal;
using Microsoft.Extensions.Logging;

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
        var serviceTypes = DependencyInjectionHelper.DependencyServices.Value;

        foreach (var service in serviceTypes)
        {
            var (named, lifeTime, interfaceTypes) = DependencyInjectionHelper.GetInjectInfos(service);

            //注册服务
            if (interfaceTypes.Length != 0)
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    if (!string.IsNullOrEmpty(named))
                    {
                        if (DependencyInjectionHelper.NamedServices.TryAdd(new Tuple<Type, string>(interfaceType, named), service))
                        {
                            DependencyInjectionHelper.Register(services, lifeTime, service);
                        }
                        else
                        {
                            throw new ArgumentException($"命名服务注册失败，接口：{interfaceType.FullName}，命名：{named} 已存在");
                        }
                    }
                    else
                    {
                        DependencyInjectionHelper.Register(services, lifeTime, service, interfaceType);
                    }
                }
            }
            else
            {
                DependencyInjectionHelper.Register(services, lifeTime, service);
            }
        }
    }
}
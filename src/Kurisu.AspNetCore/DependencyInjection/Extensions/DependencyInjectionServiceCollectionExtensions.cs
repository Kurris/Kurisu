using System;
using System.Collections.Generic;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection.Internal;
using Kurisu.AspNetCore.State.Extensions;

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
        services.RegisterStateAccessor();
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

        var namedServices = new Dictionary<Tuple<Type, string>, Type>();

        foreach (var implementType in implementTypes)
        {
            var (named, lifeTime, serviceTypes) = DependencyInjectionHelper.GetInjectInfos(implementType);

            //注册服务
            if (serviceTypes.Length != 0)
            {
                foreach (var serviceType in serviceTypes)
                {
                    if (!string.IsNullOrEmpty(named))
                    {
                        if (namedServices.TryAdd(new Tuple<Type, string>(serviceType, named), implementType))
                        {
                            DependencyInjectionHelper.Register(services, lifeTime, implementType);
                        }
                        else
                        {
                            throw new ArgumentException($"命名服务注册失败，服务类型：{serviceType.FullName}，命名：{named} 已存在");
                        }
                    }
                    else
                    {
                        DependencyInjectionHelper.Register(services, lifeTime, implementType, serviceType);
                    }
                }
            }
            else
            {
                DependencyInjectionHelper.Register(services, lifeTime, implementType);
            }
        }

        DependencyInjectionHelper.NamedServices = namedServices;
    }

    private static void RegisterStateAccessor(this IServiceCollection services)
    {
        var add = typeof(StateAccessorServiceCollectionExtensions).GetMethod(nameof(StateAccessorServiceCollectionExtensions.AddStateAccessor));
        foreach (var stateType in DependencyInjectionHelper.StateTypes.Value)
        {
            var genericAdd = add?.MakeGenericMethod(stateType);
            genericAdd?.Invoke(null, new object[] { services });
        }
    }
}
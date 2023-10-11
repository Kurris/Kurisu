using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DependencyInjection.Internal;

/// <summary>
/// 命名服务处理器
/// </summary>
[SkipScan]
internal class NamedResolver : INamedResolver
{
    private readonly IServiceProvider _serviceProvider;

    public NamedResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <param name="serviceName">服务命名</param>
    /// <returns></returns>
    public object GetService(Type type, string serviceName)
    {
        //todo interface type ->  name -- implement type
        var services = _serviceProvider.GetServices(type).ToArray();
        return services.Any() ? services.FirstOrDefault(x => GetServiceName(x.GetType()).Equals(serviceName)) : null;
    }

    /// <summary>
    /// 获取命名服务
    /// </summary>
    /// <param name="serviceName">服务命名</param>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns></returns>
    public TService GetService<TService>(string serviceName) where TService : class
    {
        return GetService(typeof(TService), serviceName) as TService;
    }

    /// <summary>
    /// 解析服务名称
    /// </summary>
    /// <param name="type">服务类型</param>
    /// <returns></returns>
    // ReSharper disable once SuggestBaseTypeForParameter
    private static string GetServiceName(Type type)
    {
        return type.IsDefined(typeof(ServiceAttribute))
            ? type.GetCustomAttribute<ServiceAttribute>()!.Named
            : type.Name;
    }
}
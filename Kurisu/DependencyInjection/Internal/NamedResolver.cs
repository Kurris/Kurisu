using System;
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
        if (DependencyInjectionHelper.NamedServices.TryGetValue(serviceName, out type))
        {
            return _serviceProvider.GetService(type);
        }
        return null;
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
}
using System;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.DependencyInjection.Internal;

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
    /// <param name="interfaceType">服务类型</param>
    /// <param name="named">服务命名</param>
    /// <returns></returns>
    public object GetService(Type interfaceType, string named)
    {
        if (DependencyInjectionHelper.NamedServices.TryGetValue(new Tuple<Type, string>(interfaceType, named), out var findType))
        {
            return _serviceProvider.GetServices(interfaceType).Where(x => x.GetType() == findType).FirstOrDefault();
        }
        return null;
    }

    /// <summary>
    /// 获取命名服务
    /// </summary>
    /// <param name="named">服务命名</param>
    /// <typeparam name="TInterface">服务类型</typeparam>
    /// <returns></returns>
    public TInterface GetService<TInterface>(string named) where TInterface : class
    {
#if NET8_0_OR_GREATER
        return _serviceProvider.GetKeyedService<TInterface>(named);
#else
        return GetService(typeof(TInterface), named) as TInterface;
#endif
    }
}
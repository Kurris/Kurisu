using System;
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
    /// 获取命名服务
    /// </summary>
    /// <param name="named">服务命名</param>
    /// <typeparam name="TInterface">服务类型</typeparam>
    /// <returns></returns>
    public TInterface GetService<TInterface>(string named) where TInterface : class
    {
        return _serviceProvider.GetKeyedService<TInterface>(named);
    }
}
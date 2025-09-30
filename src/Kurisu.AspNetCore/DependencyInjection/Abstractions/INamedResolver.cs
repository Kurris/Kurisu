using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 命名服务解析
/// </summary>
public interface INamedResolver
{
    /// <summary>
    /// 获取服务对象
    /// </summary>
    /// <param name="interfaceType">对象类型</param>
    /// <param name="named">服务名称</param>
    /// <returns></returns>
    object GetService(Type interfaceType, string named);

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <param name="named">服务名称</param>
    /// <typeparam name="TInterface">服务类型</typeparam>
    /// <returns></returns>
    TInterface GetService<TInterface>(string named) where TInterface : class;
}
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 命名服务解析
    /// </summary>
    public interface INamedResolver
    {
        /// <summary>
        /// 获取服务对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="serviceName">服务名称</param>
        /// <returns></returns>
        object GetService(Type type, string serviceName);

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns></returns>
        TService GetService<TService>(string serviceName) where TService : class;

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <typeparam name="TLifeTime">生命周期</typeparam>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns></returns>
        TService GetService<TLifeTime, TService>(string serviceName) where TLifeTime : IDependency where TService : class;
    }
}
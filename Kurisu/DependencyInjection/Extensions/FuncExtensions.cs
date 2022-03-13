using System;
using Kurisu.DependencyInjection.Abstractions;
using Kurisu.DependencyInjection.Attributes;

namespace Kurisu.DependencyInjection.Extensions
{
    /// <summary>
    /// Func扩展类 
    /// </summary>
    [SkipScan]
    public static class FuncExtensions
    {
        /// <summary>
        /// 获取瞬时命名服务的实际实例
        /// </summary>
        /// <param name="func"></param>
        /// <param name="named"></param>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService Get<TService>(this Func<string, ITransientDependency, object> func, string named) where TService : class
        {
            return func.Invoke(named, default) as TService;
        }

        /// <summary>
        /// 获取作用域命名服务的实际实例
        /// </summary>
        /// <param name="func"></param>
        /// <param name="named"></param>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService Get<TService>(this Func<string, IScopeDependency, object> func, string named) where TService : class
        {
            return func.Invoke(named, default) as TService;
        }

        /// <summary>
        /// 获取单例命名服务的实际实例
        /// </summary>
        /// <param name="func"></param>
        /// <param name="named"></param>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService Get<TService>(this Func<string, ISingletonDependency, object> func, string named) where TService : class
        {
            return func.Invoke(named, default) as TService;
        }
    }
}
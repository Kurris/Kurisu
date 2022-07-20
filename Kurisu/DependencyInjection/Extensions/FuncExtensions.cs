using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 命名服务获取扩展类 
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
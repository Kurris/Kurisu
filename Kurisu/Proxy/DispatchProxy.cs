using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Kurisu.Proxy
{
    /// <summary>
    /// 异步代理分发类
    /// </summary>
    public class DispatchProxy
    {
  
        /// <summary>
        /// 创建代理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TProxy"></typeparam>
        /// <returns></returns>
        internal static T Create<T, TProxy>() where TProxy : DispatchProxy
        {
            return (T)DispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
        }

        /// <summary>
        /// 执行同步代理
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual object Invoke(MethodInfo method, object[] args)
        {
            return method.Invoke(Target, args);
        }

        /// <summary>
        /// 执行异步代理
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task InvokeAsync(MethodInfo method, object[] args)
        {
            var task = method.Invoke(Target, args) as Task;
            return task;
        }

        /// <summary>
        /// 执行异步返回 Task{T} 代理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task<T> InvokeAsyncT<T>(MethodInfo method, object[] args)
        {
            var task = method.Invoke(Target, args) as Task<T>;
            return task;
        }

        /// <summary>
        /// 服务对象
        /// </summary>
        public object Target { get; set; }

        /// <summary>
        /// 服务提供者
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }
    }
}
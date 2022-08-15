using System;
using System.Threading.Tasks;
using Kurisu.Scope.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Scope.Internal
{
    /// <summary>
    /// 请求中的作用域,在请求结束后释放
    /// <remarks>
    /// 不等于HttpContext.RequireSerivces;有App静态类管理
    /// </remarks>
    /// </summary>
    internal class RequestScope : IScope
    {
        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="handler">异步方法</param>
        public async Task CreateAsync(Func<IServiceProvider, Task> handler)
        {
            var scope = App.GetServiceProvider(true).CreateScope();
            await handler.Invoke(scope.ServiceProvider);
        }

        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="handler">同步方法</param>
        public void Create(Action<IServiceProvider> handler)
        {
            var scope = App.GetServiceProvider(true).CreateScope();
            handler.Invoke(scope.ServiceProvider);
        }

        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="handler">同步方法</param>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns></returns>
        public TResult Create<TResult>(Func<IServiceProvider, TResult> handler)
        {
            var scope = App.GetServiceProvider(true).CreateScope();
            return handler.Invoke(scope.ServiceProvider);
        }


        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="handler">异步方法</param>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns></returns>
        public async Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
        {
            var scope = App.GetServiceProvider(true).CreateScope();
            return await handler.Invoke(scope.ServiceProvider);
        }
    }
}
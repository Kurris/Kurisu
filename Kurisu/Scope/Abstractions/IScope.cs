using System;
using System.Threading.Tasks;

namespace Kurisu.Scope.Abstractions
{
    /// <summary>
    /// 作用域
    /// </summary>
    public interface IScope
    {
        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="handler">异步方法</param>
        Task CreateAsync(Func<IServiceProvider, Task> handler);

        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="handler">同步方法</param>
        void Create(Action<IServiceProvider> handler);

        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="handler">同步方法</param>
        /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
        /// <returns></returns>
        TResult Create<TResult>(Func<IServiceProvider, TResult> handler);


        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="handler">异步方法</param>
        /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
        /// <returns></returns>
        Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler);
    }
}
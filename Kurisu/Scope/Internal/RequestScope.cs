using System;
using System.Threading.Tasks;
using Kurisu.Scope.Abstractions;

namespace Kurisu.Scope.Internal;

/// <summary>
/// 请求中的作用域,在请求结束后释放
/// <remarks>
/// 不等于<see cref="HttpContext.RequestServices"/>;创建的Scope由App框架控制释放
/// </remarks>
/// </summary>s
internal class RequestScope : IScope
{
    /// <summary>
    /// 创建作用域范围
    /// </summary>
    /// <param name="handler">异步方法</param>
    public async Task CreateAsync(Func<IServiceProvider, Task> handler)
    {
        await handler.Invoke(App.GetServiceProvider(true));
    }

    /// <summary>
    /// 创建作用域范围
    /// </summary>
    /// <param name="handler">同步方法</param>
    public void Create(Action<IServiceProvider> handler)
    {
        handler.Invoke(App.GetServiceProvider(true));
    }

    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">同步方法</param>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <returns></returns>
    public TResult Create<TResult>(Func<IServiceProvider, TResult> handler)
    {
        return handler.Invoke(App.GetServiceProvider(true));
    }


    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">异步方法</param>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <returns></returns>
    public async Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
    {
        return await handler.Invoke(App.GetServiceProvider(true));
    }
}
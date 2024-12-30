using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Scope.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.AspNetCore.Scope.Internal;

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
        var sp = App.GetServiceProvider();
        await handler.Invoke(sp);
    }

    /// <summary>
    /// 创建作用域范围
    /// </summary>
    /// <param name="handler">同步方法</param>
    public void Create(Action<IServiceProvider> handler)
    {
        var sp = App.GetServiceProvider();
        handler.Invoke(sp);
    }

    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">同步方法</param>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <returns></returns>
    public TResult Create<TResult>(Func<IServiceProvider, TResult> handler)
    {
        var sp = App.GetServiceProvider();
        return handler.Invoke(sp);
    }


    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">异步方法</param>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <returns></returns>
    public async Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
    {
        var sp = App.GetServiceProvider();
        return await handler.Invoke(sp);
    }
}
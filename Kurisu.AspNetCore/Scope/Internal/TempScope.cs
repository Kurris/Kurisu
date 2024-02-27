using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Scope.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Scope.Internal;

/// <summary>
/// 临时的作用域,在handler方法结束后释放
/// </summary>
internal class TempScope : IScope
{
    /// <summary>
    /// 创建作用域范围
    /// </summary>
    /// <param name="handler">异步方法</param>
    public async Task CreateAsync(Func<IServiceProvider, Task> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        await handler.Invoke(scope.ServiceProvider);
    }

    /// <summary>
    /// 创建作用域范围
    /// </summary>
    /// <param name="handler">同步方法</param>
    public void Create(Action<IServiceProvider> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        handler.Invoke(scope.ServiceProvider);
    }

    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">同步方法</param>
    /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
    /// <returns></returns>
    public TResult Create<TResult>(Func<IServiceProvider, TResult> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        return handler.Invoke(scope.ServiceProvider);
    }


    /// <summary>
    /// 创建作用域范围,带有返回值
    /// </summary>
    /// <param name="handler">异步方法</param>
    /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
    /// <returns></returns>
    public async Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        return await handler.Invoke(scope.ServiceProvider);
    }
}
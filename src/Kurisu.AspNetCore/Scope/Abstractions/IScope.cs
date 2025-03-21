using System;
using System.Threading.Tasks;

namespace Kurisu.AspNetCore.Scope.Abstractions;

/// <summary>
/// 作用域
/// </summary>
public interface IScope
{
    /// <summary>
    /// 处理
    /// </summary>
    /// <param name="handler">同步方法</param>
    void Invoke(Action<IServiceProvider> handler);
    
    /// <summary>
    /// 处理
    /// </summary>
    /// <param name="handler">异步方法</param>
    Task InvokeAsync(Func<IServiceProvider, Task> handler);

    /// <summary>
    /// 处理,带有返回值
    /// </summary>
    /// <param name="handler">同步方法</param>
    /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
    /// <returns></returns>
    TResult Invoke<TResult>(Func<IServiceProvider, TResult> handler);
    
    /// <summary>
    /// 处理,带有返回值
    /// </summary>
    /// <param name="handler">异步方法</param>
    /// <typeparam name="TResult">返回值类型,不可为Scope创建的对象</typeparam>
    /// <returns></returns>
    Task<TResult> InvokeAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler);
}
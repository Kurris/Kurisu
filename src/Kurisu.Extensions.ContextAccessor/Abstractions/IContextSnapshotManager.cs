using System;
using System.Threading.Tasks;

namespace Kurisu.Extensions.ContextAccessor.Abstractions;

/// <summary>
/// 上下文快照管理
/// </summary>
/// <remarks>
/// 进入下一个scope前,把当前context缓存,直到scope结束再次pop,current为当前状态
/// </remarks>
/// <typeparam name="TContext"></typeparam>
public interface IContextSnapshotManager<out TContext> where TContext : class, IContextable<TContext>, new()
{
    /// <summary>
    /// 当前context数据
    /// </summary>
    IContextAccessor<TContext> ContextAccessor { get; }

    /// <summary>
    /// context快照作用域
    /// </summary>
    /// <param name="setter"></param>
    /// <returns></returns>
    IDisposable CreateScope(Action<TContext> setter, Action onDispose = null);

    /// <summary>
    /// context快照作用域
    /// </summary>
    /// <param name="setter"></param>
    /// <param name="onDispose"></param>
    /// <returns></returns>
    IAsyncDisposable CreateScopeAsync(Action<TContext> setter, Func<Task> onDispose);
}
namespace Kurisu.AspNetCore.Abstractions.State;

public interface IStateSnapshotManager<out TState> where TState : class, ICopyable<TState>, new()
{
    /// <summary>
    /// 当前state数据
    /// </summary>
    TState Current { get; }

    /// <summary>
    /// state快照作用域
    /// </summary>
    /// <param name="setState"></param>
    /// <returns></returns>
    IDisposable BeginScope(Action<TState> setState);

    /// <summary>
    /// state快照作用域
    /// </summary>
    IAsyncDisposable BeginScopeAsync(Action<TState> setState);
}
using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.State;
using Kurisu.AspNetCore.State.Internal;

namespace Kurisu.AspNetCore.State;

/// <summary>
/// 数据状态快照管理器
/// </summary>
/// <typeparam name="TState"></typeparam>
public class StateSnapshotManager<TState> : IStateSnapshotManager<TState> where TState : class, ICopyable<TState>, new()
{
    private readonly IStateAccessor<TState> _stateAccessor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stateAccessor"></param>
    public StateSnapshotManager(IStateAccessor<TState> stateAccessor)
    {
        _stateAccessor = stateAccessor;
    }

    /// <summary>
    /// 当前state数据
    /// </summary>
    public TState Current => _stateAccessor.Current;

    /// <summary>
    /// state快照作用域 
    /// </summary>
    /// <param name="setState"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IDisposable BeginScope(Action<TState> setState)
    {
        if (setState == null) throw new ArgumentNullException(nameof(setState));
        var state = _stateAccessor.Current;
        var snapshot = new TempState<TState>(state);

        setState(state);

        return new DisposableAction(() => snapshot.RestoreTo(state));
    }

    /// <summary>
    /// state快照作用域
    /// </summary>
    /// <param name="setState"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IAsyncDisposable BeginScopeAsync(Action<TState> setState)
    {
        if (setState == null) throw new ArgumentNullException(nameof(setState));
        var state = _stateAccessor.Current;
        var snapshot = new TempState<TState>(state);

        setState(state);

        return new AsyncDisposableAction(() =>
        {
            snapshot.RestoreTo(state);
            return Task.CompletedTask;
        });
    }
}
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

public class StateSnapshotManager<TState> : IStateSnapshotManager<TState> where TState : class, ICloneable, new()
{
    private readonly AsyncLocal<TState> _current = new();
    private readonly object _createLock = new();

    /// <summary>
    /// Get the current async-context state. If not present, create a fresh baseline TState instance for this async-context.
    /// </summary>
    public TState Current => GetOrCreateCurrentState();

    private TState GetOrCreateCurrentState()
    {
        var value = _current.Value;
        if (value != null)
            return value;

        lock (_createLock)
        {
            // double-check
            if (_current.Value != null)
                return _current.Value;

            // Create a fresh baseline TState for this async-context.
            var clone = new TState();
            _current.Value = clone;
            return clone;
        }
    }


    /// <summary>
    /// Begin a temporary state scope. Returns an IDisposable which will restore the previous state when disposed.
    /// Useful for switching "name" or similar on the current request scope.
    /// </summary>
    public IDisposable BeginScope(Action<TState> setState)
    {
        if (setState == null) throw new ArgumentNullException(nameof(setState));
        var state = GetOrCreateCurrentState();
        var snapshot = new TempState<TState>(state);
        setState(state);
        return new DisposableAction(() => snapshot.RestoreTo(state));
    }

    /// <summary>
    /// Async version of BeginScope (returns an IAsyncDisposable for use with await using).
    /// </summary>
    public IAsyncDisposable BeginScopeAsync(Action<TState> setState)
    {
        if (setState == null) throw new ArgumentNullException(nameof(setState));
        var state = GetOrCreateCurrentState();
        var snapshot = new TempState<TState>(state);
        setState(state);
        return new AsyncDisposableAction(() =>
        {
            snapshot.RestoreTo(state);
            return Task.CompletedTask;
        });
    }

    public void Remove()
    {
        _current.Value = null;
    }
    
    public void Initialize()
    {
        _current.Value = new TState();
    }



    // Small helper disposable wrapper
    private sealed class DisposableAction : IDisposable
    {
        private Action _action;
        public DisposableAction(Action action) => _action = action;

        public void Dispose()
        {
            var a = _action;
            if (a == null) return;
            _action = null;
            a();
        }
    }

    private sealed class AsyncDisposableAction : IAsyncDisposable
    {
        private Func<Task> _action;
        public AsyncDisposableAction(Func<Task> action) => _action = action;

        public async ValueTask DisposeAsync()
        {
            var a = _action;
            if (a == null) return;
            _action = null;
            await a().ConfigureAwait(false);
        }
    }
}

internal record TempState<T> where T : ICloneable
{
    private static readonly PropertyInfo[] SPropertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private readonly object[] _values;
    private readonly PropertyInfo[] _propertyInfos;

    public TempState(T state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        // Clone the entire state via ICloneable
        var clonedState = (T)state.Clone();
        _propertyInfos = SPropertyInfos;
        _values = new object[_propertyInfos.Length];
        for (var i = 0; i < _propertyInfos.Length; i++)
        {
            _values[i] = _propertyInfos[i].GetValue(clonedState);
        }
    }

    public void RestoreTo(T s)
    {
        if (s == null) return;
        for (var i = 0; i < _propertyInfos.Length; i++)
        {
            var propertyInfo = _propertyInfos[i];
            if (!propertyInfo.CanWrite) continue;
            propertyInfo.SetValue(s, _values[i]);
        }
    }
}
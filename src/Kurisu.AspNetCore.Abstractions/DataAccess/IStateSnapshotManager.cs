namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public interface IStateSnapshotManager<TState> : IStateSnapshotLifecycle where TState : class, ICloneable, new()
{
    /// <summary>
    /// The current async-context state instance (created on first access if missing).
    /// </summary>
    TState Current { get; }

    /// <summary>
    /// Begin a temporary state scope. The provided <c>setState</c> mutates the current state instance; the returned IDisposable will restore the previous property values when disposed.
    /// Note: this performs a shallow, property-level snapshot/restore (does not deep-copy nested mutable objects) unless TState.Clone provides deep-copy semantics.
    /// </summary>
    IDisposable BeginScope(Action<TState> setState);

    /// <summary>
    /// Async version of BeginScope for use with <c>await using</c>.
    /// </summary>
    IAsyncDisposable BeginScopeAsync(Action<TState> setState);

    /// <summary>
    /// Clear the current async-context state.
    /// </summary>
    void Remove();

    /// <summary>
    /// Initialize the current async-context state with a fresh default <c>TState</c>.
    /// </summary>
    void Initialize();
}
namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// Non-generic lifecycle contract for state snapshot managers.
/// Implemented by <see cref="IStateSnapshotManager{TState}"/> so middleware can resolve all managers as this non-generic type.
/// </summary>
public interface IStateSnapshotLifecycle
{
    /// <summary>
    /// Initialize the current async-context state (called at request start).
    /// </summary>
    void Initialize();

    /// <summary>
    /// Remove/clear the current async-context state (called at request end).
    /// </summary>
    void Remove();
}

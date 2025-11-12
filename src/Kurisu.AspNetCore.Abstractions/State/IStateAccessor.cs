namespace Kurisu.AspNetCore.Abstractions.State;

public interface IStateAccessor<out TState> : IStateLifecycle where TState : class, new()
{
    TState Current { get; }
}
namespace Kurisu.AspNetCore.Abstractions.State;

public interface IStateLifecycle
{
    void Initialize();
    void Remove();
}
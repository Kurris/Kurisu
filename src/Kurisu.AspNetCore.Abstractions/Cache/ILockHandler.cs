namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ILockHandler : IAsyncDisposable
{
    public bool Acquired { get; }
}

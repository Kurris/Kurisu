namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

public interface ILockHandler : IAsyncDisposable
{
    public bool Acquired { get; }
}

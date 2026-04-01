namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ILockable
{
    Task<ILockHandler> LockAsync(string lockKey, TimeSpan? expiry = null, TimeSpan? retryInterval = null, int retryCount = 3);
}

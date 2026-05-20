namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

public interface ITryLockKey
{
    string GetKey();
}

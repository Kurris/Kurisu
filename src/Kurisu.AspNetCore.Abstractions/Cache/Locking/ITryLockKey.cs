namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ITryLockKey
{
    string GetKey();
}

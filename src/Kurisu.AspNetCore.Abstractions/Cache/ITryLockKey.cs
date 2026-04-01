namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ITryLockKey
{
    public string GetKey(IServiceProvider serviceProvider);
}

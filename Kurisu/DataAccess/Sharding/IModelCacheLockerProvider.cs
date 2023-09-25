using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.DataAccess.Sharding;

public interface IModelCacheLockerProvider
{
    int GetCacheModelLockObjectSeconds();

    CacheItemPriority GetCacheItemPriority();

    int GetCacheEntrySize();

    object GetCacheLockObject(object modelCacheKey);
}
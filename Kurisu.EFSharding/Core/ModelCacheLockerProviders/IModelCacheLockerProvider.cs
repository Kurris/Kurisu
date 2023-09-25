using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.EFSharding.Core.ModelCacheLockerProviders;

public interface IModelCacheLockerProvider
{
    int GetCacheModelLockObjectSeconds();

    CacheItemPriority GetCacheItemPriority();
    int GetCacheEntrySize();

    object GetCacheLockObject(object modelCacheKey);
}
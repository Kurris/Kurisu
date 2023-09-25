using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.ModelCacheLockerProviders;
using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.Test.WebApi_A;

public class DicModelCacheLockerProvider : IModelCacheLockerProvider
{
    private readonly ConcurrentDictionary<string, object> _locks = new();

    public int GetCacheModelLockObjectSeconds()
    {
        return 20;
    }

    public CacheItemPriority GetCacheItemPriority()
    {
        return CacheItemPriority.High;
    }

    public int GetCacheEntrySize()
    {
        return 1;
    }

    public object GetCacheLockObject(object modelCacheKey)
    {
        var key = $"{modelCacheKey}";
        if (!_locks.TryGetValue(key, out var obj))
        {
            obj = new object();
            if (_locks.TryAdd(key, obj))
            {
                return obj;
            }

            if (!_locks.TryGetValue(key, out obj))
            {
                throw new Exception("no lock");
            }
        }

        return obj;
    }
}
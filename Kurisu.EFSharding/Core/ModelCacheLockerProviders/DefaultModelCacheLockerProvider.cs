using Kurisu.EFSharding.Core.ShardingConfigurations;
using Kurisu.EFSharding.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace Kurisu.EFSharding.Core.ModelCacheLockerProviders;

public class DefaultModelCacheLockerProvider : IModelCacheLockerProvider
{
    private readonly ShardingOptions _shardingConfigOptions;
    private readonly object[] _locks;

    public DefaultModelCacheLockerProvider(ShardingOptions shardingConfigOptions)
    {
        _shardingConfigOptions = shardingConfigOptions;
        if (shardingConfigOptions.CacheModelLockConcurrencyLevel <= 0)
        {
            throw new ShardingCoreInvalidOperationException(
                $"{shardingConfigOptions.CacheModelLockConcurrencyLevel} should > 0");
        }

        _locks = new object [shardingConfigOptions.CacheModelLockConcurrencyLevel];
        for (var i = 0; i < shardingConfigOptions.CacheModelLockConcurrencyLevel; i++)
        {
            _locks[i] = new object();
        }
    }

    public int GetCacheModelLockObjectSeconds()
    {
        return _shardingConfigOptions.CacheModelLockObjectSeconds;
    }

    public CacheItemPriority GetCacheItemPriority()
    {
        return _shardingConfigOptions.CacheItemPriority;
    }

    public int GetCacheEntrySize()
    {
        return _shardingConfigOptions.CacheEntrySize;
    }

    public object GetCacheLockObject(object modelCacheKey)
    {
        if (modelCacheKey == null)
        {
            throw new ShardingCoreInvalidOperationException(
                $"modelCacheKey is null cant {nameof(GetCacheLockObject)}");
        }

        if (_locks.Length == 1)
        {
            return _locks[0];
        }

        var hashCode = (modelCacheKey.ToString() ?? "").GetHashCode();
        var index = Math.Abs(hashCode % _locks.Length);
        return _locks[index];
    }
}
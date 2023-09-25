using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.ShardingConfigurations;

namespace Kurisu.EFSharding.Core.TrackerManagers;

public class TrackerManager : ITrackerManager
{
    private readonly ShardingOptions _shardingConfigOptions;
    private readonly ConcurrentDictionary<Type, bool> _dbContextModels = new();

    public TrackerManager(ShardingOptions shardingConfigOptions)
    {
        _shardingConfigOptions = shardingConfigOptions;
    }

    public bool AddDbContextModel(Type entityType, bool hasKey)
    {
        return _dbContextModels.TryAdd(entityType, hasKey);
    }

    public bool EntityUseTrack(Type entityType)
    {
        if (_dbContextModels.TryGetValue(entityType, out var hasKey))
        {
            return hasKey;
        }

        if (_shardingConfigOptions.UseEntityFrameworkCoreProxies && entityType.BaseType != null)
        {
            if (_dbContextModels.TryGetValue(entityType.BaseType, out hasKey))
            {
                return hasKey;
            }
        }

        return false;
    }

    public bool IsDbContextModel(Type entityType)
    {
        if (_dbContextModels.ContainsKey(entityType))
        {
            return true;
        }

        if (_shardingConfigOptions.UseEntityFrameworkCoreProxies && entityType.BaseType != null)
        {
            return _dbContextModels.ContainsKey(entityType.BaseType);
        }

        return false;
    }

    public Type TranslateEntityType(Type entityType)
    {
        if (_shardingConfigOptions.UseEntityFrameworkCoreProxies)
        {
            if (!_dbContextModels.ContainsKey(entityType))
            {
                if (entityType.BaseType != null)
                {
                    if (_dbContextModels.ContainsKey(entityType.BaseType))
                    {
                        return entityType.BaseType;
                    }
                }
            }
        }

        return entityType;
    }
}
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails;

public class MultiQueryRouteTail:IMultiQueryRouteTail
{
    public const string RANDOM_MODEL_CACHE_KEY = "RANDOM_SHARDING_MODEL_CACHE_KEY";
    private readonly TableRouteResult _tableRouteResult;
    private readonly string _modelCacheKey;
    private readonly ISet<Type> _entityTypes;
    private readonly bool _isShardingTableQuery;

    public MultiQueryRouteTail(TableRouteResult tableRouteResult,bool isShardingTableQuery)
    {
        if (tableRouteResult.ReplaceTables.IsEmpty() || tableRouteResult.ReplaceTables.Count <= 1) throw new ArgumentException("route result replace tables must greater than  1");
        _tableRouteResult = tableRouteResult;
        _modelCacheKey = RANDOM_MODEL_CACHE_KEY+Guid.NewGuid().ToString("n");
        _entityTypes = tableRouteResult.ReplaceTables.Select(o=>o.EntityType).ToHashSet();
        _isShardingTableQuery = isShardingTableQuery;
    }
    public string GetRouteTailIdentity()
    {
        return _modelCacheKey;
    }

    public bool IsMultiEntityQuery()
    {
        return true;
    }

    public bool IsShardingTableQuery()
    {
        return _isShardingTableQuery;
    }

    public string GetEntityTail(Type entityType)
    {
        return _tableRouteResult.ReplaceTables.Single(o => o.EntityType == entityType).Tail;
    }

    public ISet<Type> GetEntityTypes()
    {
        return _entityTypes;
    }
}
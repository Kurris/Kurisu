using System.Linq.Expressions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;

public class ShardingTableRouteConfig
{
    private readonly IQueryable _queryable;
    private readonly object _shardingTable;
    private readonly object _shardingKeyValue;
    private readonly Expression _predicate;


    public ShardingTableRouteConfig(IQueryable queryable = null, object shardingTable = null, object shardingKeyValue = null, Expression predicate = null)
    {
        _queryable = queryable;
        _shardingTable = shardingTable;
        _shardingKeyValue = shardingKeyValue;
        _predicate = predicate;
    }

    public IQueryable GetQueryable()
    {
        return _queryable;
    }

    public object GetShardingKeyValue()
    {
        return _shardingKeyValue;
    }

    public object GetShardingEntity()
    {
        return _shardingTable;
    }

    public Expression GetPredicate()
    {
        return _predicate;
    }

    public bool UseQueryable()
    {
        return _queryable != null;
    }

    public bool UseValue()
    {
        return _shardingKeyValue != null;
    }

    public bool UseEntity()
    {
        return _shardingTable != null;
    }

    public bool UsePredicate()
    {
        return _predicate != null;
    }
}
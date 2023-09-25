using System.Linq.Expressions;

namespace Kurisu.EFSharding.Core.VirtualRoutes;

public class ShardingDatasourceRouteConfig
{

    private readonly IQueryable _queryable;
    private readonly object _shardingdatasource;
    private readonly object _shardingKeyValue;
    private readonly Expression _predicate;


    public ShardingDatasourceRouteConfig(IQueryable queryable=null,object shardingDatasource=null,object shardingKeyValue=null,Expression predicate=null)
    {
        _queryable = queryable;
        _shardingdatasource = shardingDatasource;
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

    public object GetShardingdatasource()
    {
        return _shardingdatasource;
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
        return _shardingdatasource != null;
    }

    public bool UsePredicate()
    {
        return _predicate != null;
    }
}
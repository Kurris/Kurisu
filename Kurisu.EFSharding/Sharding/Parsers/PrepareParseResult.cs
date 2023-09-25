using System.Linq.Expressions;
using Kurisu.EFSharding.Core;
using Kurisu.EFSharding.Core.QueryRouteManagers;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Visitors;

namespace Kurisu.EFSharding.Sharding.Parsers;

public class PrepareParseResult : IPrepareParseResult
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly Expression _nativeQueryExpression;
    private readonly bool _useUnionAllMerge;
    private readonly int? _maxQueryConnectionsLimit;
    private readonly ConnectionModeEnum? _connectionMode;
    private readonly bool? _readOnly;
    private readonly Action<ShardingRouteContext> _shardingRouteConfigure;
    private readonly bool? _isSequence;
    private readonly bool? _isNoTracking;
    private readonly bool _isIgnoreFilter;
    private readonly bool? _sameWithShardingComparer;
    private readonly Dictionary<Type, IQueryable> _queryEntities;

    public PrepareParseResult(IShardingDbContext shardingDbContext, Expression nativeQueryExpression, ShardingPrepareResult shardingPrepareResult)
    {
        _shardingDbContext = shardingDbContext;
        _nativeQueryExpression = nativeQueryExpression;
        _shardingRouteConfigure = shardingPrepareResult.ShardingQueryableAsRouteOptions?.RouteConfigure;
        _useUnionAllMerge = shardingPrepareResult.UseUnionAllMerge;
        _maxQueryConnectionsLimit = shardingPrepareResult.ShardingQueryableUseConnectionModeOptions?.MaxQueryConnectionsLimit;

        // if (shardingDbContext.IsUseReadWriteSeparation())
        // {
        //     _readOnly = shardingPrepareResult?.ShardingQueryableReadWriteSeparationOptions?.RouteReadConnect ?? shardingDbContext.CurrentIsReadWriteSeparation();
        // }

        _isSequence = shardingPrepareResult.ShardingQueryableAsSequenceOptions?.AsSequence;
        _sameWithShardingComparer = shardingPrepareResult.ShardingQueryableAsSequenceOptions
            ?.SameWithShardingComparer;
        _queryEntities = shardingPrepareResult.QueryEntities;
        _isNoTracking = shardingPrepareResult.IsNoTracking;
        _isIgnoreFilter = shardingPrepareResult.IsIgnoreFilter;
    }

    public IShardingDbContext GetShardingDbContext()
    {
        return _shardingDbContext;
    }

    public Expression GetNativeQueryExpression()
    {
        return _nativeQueryExpression;
    }

    public bool UseUnionAllMerge()
    {
        return _useUnionAllMerge;
    }

    public int? GetMaxQueryConnectionsLimit()
    {
        return _maxQueryConnectionsLimit;
    }


    public bool? ReadOnly()
    {
        return _readOnly;
    }

    public Action<ShardingRouteContext> GetAsRoute()
    {
        return _shardingRouteConfigure;
    }

    public bool? IsSequence()
    {
        return _isSequence;
    }

    public bool? SameWithShardingComparer()
    {
        return _sameWithShardingComparer;
    }

    public Dictionary<Type, IQueryable> GetQueryEntities()
    {
        return _queryEntities;
    }

    public bool? IsNotracking()
    {
        return _isNoTracking;
    }

    public bool IsIgnoreFilter()
    {
        return _isIgnoreFilter;
    }

    public override string ToString()
    {
        return $"query entity types :{string.Join(",", _queryEntities.Keys)},is no tracking: {_isNoTracking},is ignore filter :{_isIgnoreFilter},is not support :{_useUnionAllMerge},max query connections limit:{_maxQueryConnectionsLimit},connection mode:{_connectionMode},readonly:{_readOnly},as route:{_shardingRouteConfigure != null},is sequence:{_isSequence},same with sharding comparer:{_sameWithShardingComparer}";
    }
}
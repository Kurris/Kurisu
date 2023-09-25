using Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

namespace Kurisu.EFSharding.Sharding.Parsers.Visitors;

public class ShardingPrepareResult
{
    public bool UseUnionAllMerge { get; }
    public ShardingQueryableAsRouteOptions ShardingQueryableAsRouteOptions { get; }
    public ShardingQueryableUseConnectionModeOptions ShardingQueryableUseConnectionModeOptions { get; }
    public ShardingQueryableReadWriteSeparationOptions ShardingQueryableReadWriteSeparationOptions { get; }
    public ShardingQueryableAsSequenceOptions ShardingQueryableAsSequenceOptions { get; }
    public Dictionary<Type, IQueryable> QueryEntities { get; }
    public bool? IsNoTracking { get; }
    public bool IsIgnoreFilter { get; }


    public ShardingPrepareResult(bool useUnionAllMerge,
        ShardingQueryableAsRouteOptions shardingQueryableAsRouteOptions,
        ShardingQueryableUseConnectionModeOptions shardingQueryableUseConnectionModeOptions,
        ShardingQueryableReadWriteSeparationOptions shardingQueryableReadWriteSeparationOptions,
        ShardingQueryableAsSequenceOptions shardingQueryableAsSequenceOptions, Dictionary<Type, IQueryable> queryEntities,
        bool? isNoTracking, bool isIgnoreFilter)
    {
        UseUnionAllMerge = useUnionAllMerge;
        ShardingQueryableAsRouteOptions = shardingQueryableAsRouteOptions;
        ShardingQueryableUseConnectionModeOptions = shardingQueryableUseConnectionModeOptions;
        ShardingQueryableReadWriteSeparationOptions = shardingQueryableReadWriteSeparationOptions;
        ShardingQueryableAsSequenceOptions = shardingQueryableAsSequenceOptions;
        QueryEntities = queryEntities;
        IsNoTracking = isNoTracking;
        IsIgnoreFilter = isIgnoreFilter;
    }
}
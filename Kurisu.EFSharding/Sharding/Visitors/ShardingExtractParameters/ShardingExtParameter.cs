using Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

namespace Kurisu.EFSharding.Sharding.Visitors.ShardingExtractParameters;

internal class ShardingExtParameter
{
    public bool UseUnionAllMerge { get; }
    public ShardingQueryableAsRouteOptions ShardingQueryableAsRouteOptions { get; }
    public ShardingQueryableUseConnectionModeOptions ShardingQueryableUseConnectionModeOptions { get; }
    public ShardingQueryableReadWriteSeparationOptions ShardingQueryableReadWriteSeparationOptions { get; }
    public ShardingQueryableAsSequenceOptions ShardingQueryableAsSequenceOptions { get; }

    public ShardingExtParameter(bool useUnionAllMerge,
        ShardingQueryableAsRouteOptions shardingQueryableAsRouteOptions,
        ShardingQueryableUseConnectionModeOptions shardingQueryableUseConnectionModeOptions,
        ShardingQueryableReadWriteSeparationOptions shardingQueryableReadWriteSeparationOptions,
        ShardingQueryableAsSequenceOptions shardingQueryableAsSequenceOptions)
    {
        UseUnionAllMerge = useUnionAllMerge;
        ShardingQueryableAsRouteOptions = shardingQueryableAsRouteOptions;
        ShardingQueryableUseConnectionModeOptions = shardingQueryableUseConnectionModeOptions;
        ShardingQueryableReadWriteSeparationOptions = shardingQueryableReadWriteSeparationOptions;
        ShardingQueryableAsSequenceOptions = shardingQueryableAsSequenceOptions;
    }
}
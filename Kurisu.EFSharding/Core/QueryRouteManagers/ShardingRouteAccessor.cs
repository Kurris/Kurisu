using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

namespace Kurisu.EFSharding.Core.QueryRouteManagers;

public class ShardingRouteAccessor: IShardingRouteAccessor
{
    private static AsyncLocal<ShardingRouteContext> _shardingRouteContext = new AsyncLocal<ShardingRouteContext>();

    /// <summary>
    /// sharding route context use in using code block
    /// </summary>
    public ShardingRouteContext ShardingRouteContext
    {
        get => _shardingRouteContext.Value;
        set => _shardingRouteContext.Value = value;
    }

}
namespace Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

public interface IShardingRouteAccessor
{
    ShardingRouteContext ShardingRouteContext { get; set; }
}
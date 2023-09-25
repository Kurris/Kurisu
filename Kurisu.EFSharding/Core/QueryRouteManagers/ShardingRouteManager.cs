using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

namespace Kurisu.EFSharding.Core.QueryRouteManagers;


public class ShardingRouteManager: IShardingRouteManager
{
    private readonly IShardingRouteAccessor _shardingRouteAccessor;

    public ShardingRouteManager(IShardingRouteAccessor shardingRouteAccessor)
    {
        _shardingRouteAccessor = shardingRouteAccessor;
    }

    public ShardingRouteContext Current => _shardingRouteAccessor.ShardingRouteContext;
    public ShardingRouteScope CreateScope()
    {
        var shardingRouteScope = new ShardingRouteScope(_shardingRouteAccessor);
        _shardingRouteAccessor.ShardingRouteContext = ShardingRouteContext.Create();
        return shardingRouteScope;
    }
}
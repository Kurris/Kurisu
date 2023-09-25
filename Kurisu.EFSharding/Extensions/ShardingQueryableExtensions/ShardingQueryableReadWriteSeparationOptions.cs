namespace Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

public class ShardingQueryableReadWriteSeparationOptions
{
    public bool RouteReadConnect { get; }

    public ShardingQueryableReadWriteSeparationOptions(bool routeReadConnect)
    {
        RouteReadConnect = routeReadConnect;
    }
}
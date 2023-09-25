using Kurisu.EFSharding.Core.QueryRouteManagers;

namespace Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

public class ShardingQueryableAsRouteOptions
{
    public Action<ShardingRouteContext> RouteConfigure { get; }

    public ShardingQueryableAsRouteOptions(Action<ShardingRouteContext> routeConfigure)
    {
        RouteConfigure = routeConfigure;
    }
}
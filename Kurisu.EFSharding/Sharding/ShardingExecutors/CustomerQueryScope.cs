using Kurisu.EFSharding.Core.QueryRouteManagers;
using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors;

internal class CustomerQueryScope:IDisposable
{
    private readonly ShardingRouteScope _shardingRouteScope;
    private readonly bool _hasCustomerQuery;
    public CustomerQueryScope(IPrepareParseResult prepareParseResult,IShardingRouteManager shardingRouteManager)
    {
        _hasCustomerQuery = prepareParseResult.HasCustomerQuery();
        if (_hasCustomerQuery)
        {
            var asRoute = prepareParseResult.GetAsRoute();
            if ( asRoute!= null)
            {
                _shardingRouteScope = shardingRouteManager.CreateScope();
                asRoute.Invoke(shardingRouteManager.Current);
            }

        }
    }
    public void Dispose()
    {
        if (_hasCustomerQuery)
        {
            _shardingRouteScope?.Dispose();
        }
    }
}
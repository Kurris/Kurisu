using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Core.Metadata;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes;

internal class RouteTailFactory : IRouteTailFactory
{
    private readonly IMetadataManager _entityMetadataManager;

    public RouteTailFactory(IMetadataManager entityMetadataManager)
    {
        _entityMetadataManager = entityMetadataManager;
    }

    public IRouteTail Create(string tail)
    {
        return Create(tail, true);
    }

    public IRouteTail Create(string tail, bool cache)
    {
        if (cache)
        {
            return new SingleQueryRouteTail(tail);
        }

        return new NoCacheSingleQueryRouteTail(tail);
    }

    public IRouteTail Create(TableRouteResult tableRouteResult)
    {
        return Create(tableRouteResult, true);
    }

    public IRouteTail Create(TableRouteResult tableRouteResult, bool cache)
    {
        if (tableRouteResult == null || tableRouteResult.ReplaceTables.IsEmpty())
        {
            if (cache)
            {
                return new SingleQueryRouteTail(string.Empty);
            }

            return new NoCacheSingleQueryRouteTail(string.Empty);
        }

        if (tableRouteResult.ReplaceTables.Count == 1)
        {
            if (cache)
            {
                return new SingleQueryRouteTail(tableRouteResult);
            }
            else
            {
                return new NoCacheSingleQueryRouteTail(tableRouteResult);
            }
        }

        var isShardingTableQuery = tableRouteResult.ReplaceTables.Select(o => o.EntityType)
            .Any(o => _entityMetadataManager.IsShardingTable(o));
        return new MultiQueryRouteTail(tableRouteResult, isShardingTableQuery);
    }
}
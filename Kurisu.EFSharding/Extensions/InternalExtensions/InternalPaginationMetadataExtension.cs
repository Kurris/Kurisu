using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;

namespace Kurisu.EFSharding.Extensions.InternalExtensions;

internal  static class InternalPaginationMetadataExtension
{
    internal static bool IsUseReverse(this PaginationMetadata paginationMetadata,int skip,long total)
    {
        if (total < paginationMetadata.ReverseTotalGe)
            return false;

        return skip> paginationMetadata.ReverseFactor * total;
    }
    internal static bool IsUseUneven(this PaginationMetadata paginationMetadata,ICollection<RouteQueryResult<long>> routeQueryResults,int skip)
    {
        if (routeQueryResults.Count <= 1)
            return false;

        if (skip < paginationMetadata.UnevenLimit)
            return false;
        var total = routeQueryResults.Sum(o => o.QueryResult);
        if(total* paginationMetadata.UnevenFactorGe < routeQueryResults.First().QueryResult)
            return false;
        return true;
    }
}
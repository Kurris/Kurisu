using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;

namespace Kurisu.EFSharding.Core.ShardingPage;

public class ShardingPageContext
{
    public ICollection<RouteQueryResult<long>> RouteQueryResults { get; }

    private ShardingPageContext()
    {
        RouteQueryResults = new LinkedList<RouteQueryResult<long>>();
    }

    public static ShardingPageContext Create()
    {
        return new ShardingPageContext();
    }
}
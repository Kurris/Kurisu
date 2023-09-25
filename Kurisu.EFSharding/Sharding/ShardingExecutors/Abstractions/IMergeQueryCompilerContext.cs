using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

public interface IMergeQueryCompilerContext : IQueryCompilerContext
{
    QueryCombineResult GetQueryCombineResult();
    ShardingRouteResult GetShardingRouteResult();

    bool IsCrossTable();
    bool IsCrossDataSource();
    int? GetFixedTake();
}
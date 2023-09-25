using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

namespace Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;

public interface IUnionAllMergeManager
{
    UnionAllMergeContext Current { get; }
    /// <summary>
    /// 创建scope
    /// </summary>
    /// <returns></returns>
    UnionAllMergeScope CreateScope(IEnumerable<TableRouteResult> tableRouteResults);
}
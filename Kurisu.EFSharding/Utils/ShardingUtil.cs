using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Sharding.Visitors;

namespace Kurisu.EFSharding.Utils;

public static class ShardingUtils
{
    /// <summary>
    /// 获取表达式路由
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="metadata"></param>
    /// <param name="keyToTailExpression"></param>
    /// <returns></returns>
    public static RoutePredicateExpression GetRouteParseExpression<TKey>(IQueryable queryable
        , BaseShardingMetadata metadata
        , Func<TKey, ShardingOperatorEnum, string, Func<string, bool>> keyToTailExpression)
    {
        var visitor = new QueryableRouteShardingTableDiscoverVisitor<TKey>(metadata, keyToTailExpression);
        visitor.Visit(queryable.Expression);
        return visitor.GetRouteParseExpression();
    }
}
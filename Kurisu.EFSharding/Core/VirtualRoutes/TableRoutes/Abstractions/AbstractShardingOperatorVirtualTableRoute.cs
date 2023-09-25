using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Utils;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

public abstract class AbstractShardingOperatorVirtualTableRoute<TEntity, TKey> : BaseShardingFilterVirtualTableRoute<TEntity>
    where TEntity : class, new()
{
    protected AbstractShardingOperatorVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    protected override List<TableRouteUnit> DoRouteWithPredicate(DatasourceRouteResult dataSourceRouteResult, IQueryable queryable)
    {
        //获取路由后缀表达式
        var routeParseExpression = ShardingUtils.GetRouteParseExpression<TKey>(queryable, Metadata, GetRouteFilter);

        var filter = routeParseExpression.GetRoutePredicate();
        var sqlRouteUnits = dataSourceRouteResult.IntersectDataSources.SelectMany(dataSourceName =>
            GetTails()
                .Where(o => filter(FormatTableRouteWithDataSource(dataSourceName, o)))
                .Select(tail => new TableRouteUnit(typeof(TEntity), dataSourceName, tail))
        ).ToList();

        return sqlRouteUnits;
    }


    /// <summary>
    /// 如何路由到具体表 shardingKeyValue:分表的值, 返回结果:如果返回true表示返回该表 第一个参数 tail 第二参数是否返回该物理表
    /// </summary>
    /// <param name="shardingKey">分表的值</param>
    /// <param name="shardingOperator">操作</param>
    /// <param name="shardingPropertyName">分表字段</param>
    /// <returns>如果返回true表示返回该表 第一个参数 tail 第二参数是否返回该物理表</returns>
    protected virtual Func<string, bool> GetRouteFilter(TKey shardingKey,
        ShardingOperatorEnum shardingOperator, string shardingPropertyName)
    {
        return Metadata.IsMainShardingTableKey(shardingPropertyName)
            ? GetRouteToFilter(CompareValueToKey(shardingKey), shardingOperator)
            : GetExtraRouteFilter(shardingKey, shardingOperator, shardingPropertyName);
    }

    protected virtual TKey CompareValueToKey(TKey shardingKey)
    {
        return shardingKey;
    }

    protected abstract Func<string, bool> GetRouteToFilter(TKey shardingKey,
        ShardingOperatorEnum shardingOperator);

    protected virtual Func<string, bool> GetExtraRouteFilter(TKey shardingKey,
        ShardingOperatorEnum shardingOperator, string shardingPropertyName)
    {
        throw new NotImplementedException(shardingPropertyName);
    }

    public override TableRouteUnit RouteWithValue(DatasourceRouteResult dataSourceRouteResult, object shardingKey)
    {
        if (dataSourceRouteResult.IntersectDataSources.Count != 1)
        {
            throw new ShardingCoreException($"more than one route match data source:{string.Join(",", dataSourceRouteResult.IntersectDataSources)}");
        }

        var shardingKeyToTail = ToTail(shardingKey);

        var filterTails = GetTails().Where(o => o == shardingKeyToTail).ToList();
        if (filterTails.IsEmpty())
        {
            throw new ShardingCoreException($"sharding key route not match {Metadata.ClrType} -> [{Metadata.Property.Name}] -> [{shardingKey}] -> sharding key to tail :[{shardingKeyToTail}] ->  all tails ->[{string.Join(",", GetTails())}]");
        }

        if (filterTails.Count > 1)
            throw new ShardingCoreException($"more than one route match table:{string.Join(",", filterTails)}");
        return new TableRouteUnit(typeof(TEntity), dataSourceRouteResult.IntersectDataSources.First(), filterTails[0]);
    }
}
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Utils;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute.Abstractions;

public abstract class AbstractShardingOperatorVirtualDataSourceRoute<TEntity, TKey> : AbstractShardingFilterVirtualDataSourceRoute<TEntity, TKey>
    where TEntity : class, new()
{
    protected AbstractShardingOperatorVirtualDataSourceRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }


    protected override List<string> DoRouteWithPredicate(IEnumerable<string> allDataSourceNames, IQueryable queryable)
    {
        //获取路由后缀表达式
        var routeParseExpression = ShardingUtils.GetRouteParseExpression<TKey>(queryable, Metadata, GetRouteFilter);
        //表达式缓存编译
        // var filter = CachingCompile(routeParseExpression);
        var filter = routeParseExpression.GetRoutePredicate();
        //通过编译结果进行过滤
        var dataSources = allDataSourceNames.Where(o => filter(o)).ToList();
        return dataSources;
    }


    /// <summary>
    /// 如何路由到具体表 shardingKeyValue:分表的值, 返回结果:如果返回true表示返回该表 第一个参数 tail 第二参数是否返回该物理表
    /// </summary>
    /// <param name="shardingKey">分表的值</param>
    /// <param name="shardingOperator">操作</param>
    /// <param name="shardingPropertyName">操作</param>
    /// <returns>如果返回true表示返回该表 第一个参数 tail 第二参数是否返回该物理表</returns>
    protected virtual Func<string, bool> GetRouteFilter(TKey shardingKey,
        ShardingOperatorEnum shardingOperator, string shardingPropertyName)
    {
        return Metadata.IsMainShardingDatasourceKey(shardingPropertyName)
            ? GetRouteToFilter(shardingKey, shardingOperator)
            : GetExtraRouteFilter(shardingKey, shardingOperator, shardingPropertyName);
    }

    protected abstract Func<string, bool> GetRouteToFilter(TKey shardingKey,
        ShardingOperatorEnum shardingOperator);

    protected virtual Func<string, bool> GetExtraRouteFilter(object shardingKey,
        ShardingOperatorEnum shardingOperator, string shardingPropertyName)
    {
        throw new NotImplementedException(shardingPropertyName);
    }

    public override string RouteWithValue(object shardingKey)
    {
        var allDataSourceNames = GetAllDatasourceNames();
        var shardingKeyToDataSource = ShardingKeyToDataSourceName(shardingKey);

        var dataSources = allDataSourceNames.Where(o => o == shardingKeyToDataSource).ToList();
        if (dataSources.IsEmpty())
        {
            throw new ShardingCoreException($"sharding key route not match {Metadata.ClrType} -> [{Metadata.Property.Name}] ->【{shardingKey}】 all data sources ->[{string.Join(",", allDataSourceNames.Select(o => o))}]");
        }

        if (dataSources.Count > 1)
            throw new ShardingCoreException($"more than one route match data source:{string.Join(",", dataSources.Select(o => $"[{o}]"))}");

        return dataSources[0];
    }
}
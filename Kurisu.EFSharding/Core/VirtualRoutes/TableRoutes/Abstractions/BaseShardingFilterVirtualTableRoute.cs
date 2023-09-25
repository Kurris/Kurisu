using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.QueryRouteManagers;
using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

public abstract class BaseShardingFilterVirtualTableRoute<TEntity> : BaseVirtualTable<TEntity>
    where TEntity : class, new()
{
    protected ShardingRouteContext CurrentShardingRouteContext => ShardingProvider.GetRequiredService<IShardingRouteManager>().Current;

    /// <summary>
    /// 启用提示路由
    /// </summary>
    protected virtual bool EnableHintRoute => false;

    /// <summary>
    /// 启用断言路由
    /// </summary>
    protected virtual bool EnableAssertRoute => false;

    /// <summary>
    /// 路由是否忽略数据源
    /// </summary>
    protected virtual bool RouteIgnoreDataSource => true;

    /// <summary>
    /// 路由数据源和表后缀连接符
    /// </summary>
    protected virtual string RouteSeparator => ".";

    protected BaseShardingFilterVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    public override List<TableRouteUnit> RouteWithPredicate(DatasourceRouteResult dataSourceRouteResult, IQueryable queryable, bool isQuery)
    {
        if (!isQuery)
        {
            //后拦截器
            return DoRouteWithPredicate(dataSourceRouteResult, queryable);
        }

        //强制路由不经过断言
        if (EnableHintRoute)
        {
            if (CurrentShardingRouteContext.TryGetMustTail<TEntity>(out HashSet<string> mustTails) && mustTails.IsNotEmpty())
            {
                var filterTails = GetTails().Where(o => mustTails.Contains(o)).ToList();
                if (filterTails.IsEmpty() || filterTails.Count != mustTails.Count)
                    throw new ShardingCoreException($" sharding route must error:[{Metadata.ClrType.FullName}]-->[{string.Join(",", mustTails)}]");
                var shardingRouteUnits = dataSourceRouteResult.IntersectDataSources.SelectMany(datasourceName => filterTails.Select(tail => new TableRouteUnit(typeof(TEntity), datasourceName, tail))).ToList();
                return shardingRouteUnits;
            }

            if (CurrentShardingRouteContext.TryGetHintTail<TEntity>(out HashSet<string> hintTails) && hintTails.IsNotEmpty())
            {
                var filterTails = GetTails().Where(o => hintTails.Contains(o)).ToList();
                if (filterTails.IsEmpty() || filterTails.Count != hintTails.Count)
                    throw new ShardingCoreException($" sharding route hint error:[{Metadata.ClrType.FullName}]-->[{string.Join(",", hintTails)}]");
                var shardingRouteUnits = dataSourceRouteResult.IntersectDataSources.SelectMany(datasourceName => filterTails.Select(tail => new TableRouteUnit(typeof(TEntity), datasourceName, tail))).ToList();
                return GetFilterTableTails(dataSourceRouteResult, shardingRouteUnits);
            }
        }


        var filterPhysicTables = DoRouteWithPredicate(dataSourceRouteResult, queryable);
        return GetFilterTableTails(dataSourceRouteResult, filterPhysicTables);
    }

    /// <summary>
    /// 判断是调用全局过滤器还是调用内部断言
    /// </summary>
    /// <param name="dataSourceRouteResult"></param>
    /// <param name="shardingRouteUnits"></param>
    /// <returns></returns>
    private List<TableRouteUnit> GetFilterTableTails(DatasourceRouteResult dataSourceRouteResult, List<TableRouteUnit> shardingRouteUnits)
    {
        if (UseAssertRoute)
        {
            //最后处理断言
            ProcessAssertRoutes(dataSourceRouteResult, shardingRouteUnits);
            return shardingRouteUnits;
        }

        //后拦截器
        return AfterShardingRouteUnitFilter(dataSourceRouteResult, shardingRouteUnits);
    }

    private bool UseAssertRoute => EnableAssertRoute &&
                                   CurrentShardingRouteContext.TryGetAssertTail<TEntity>(
                                       out ICollection<ITableRouteAssert> routeAsserts) &&
                                   routeAsserts.IsNotEmpty();

    private void ProcessAssertRoutes(DatasourceRouteResult dataSourceRouteResult, List<TableRouteUnit> shardingRouteUnits)
    {
        if (UseAssertRoute)
        {
            if (CurrentShardingRouteContext.TryGetAssertTail<TEntity>(out ICollection<ITableRouteAssert> routeAsserts))
            {
                foreach (var routeAssert in routeAsserts)
                {
                    routeAssert.Assert(dataSourceRouteResult, GetTails(), shardingRouteUnits);
                }
            }
        }
    }

    protected abstract List<TableRouteUnit> DoRouteWithPredicate(DatasourceRouteResult dataSourceRouteResult, IQueryable queryable);


    /// <summary>
    /// 物理表过滤后
    /// </summary>
    /// <param name="dataSourceRouteResult">所有的数据源</param>
    /// <param name="shardingRouteUnits">所有的物理表</param>
    /// <returns></returns>
    protected virtual List<TableRouteUnit> AfterShardingRouteUnitFilter(DatasourceRouteResult dataSourceRouteResult, List<TableRouteUnit> shardingRouteUnits)
    {
        return shardingRouteUnits;
    }

    protected string FormatTableRouteWithDataSource(string datasourceName, string tableTail)
    {
        return RouteIgnoreDataSource ? tableTail : $"{datasourceName}{RouteSeparator}{tableTail}";
    }
}
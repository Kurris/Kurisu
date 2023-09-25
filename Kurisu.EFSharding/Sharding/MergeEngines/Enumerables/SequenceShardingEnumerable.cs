using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Enumerables.Base;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;

internal class SequenceShardingEnumerable<TEntity> :AbstractStreamEnumerable<TEntity>
{
    private readonly PaginationSequenceConfig _dataSourceSequenceMatchOrderConfig;
    private readonly PaginationSequenceConfig _tableSequenceMatchOrderConfig;
    private readonly ICollection<RouteQueryResult<long>> _routeQueryResults;
    private readonly bool _isAsc;
    public SequenceShardingEnumerable(StreamMergeContext streamMergeContext, PaginationSequenceConfig dataSourceSequenceMatchOrderConfig, PaginationSequenceConfig tableSequenceMatchOrderConfig, ICollection<RouteQueryResult<long>> routeQueryResults, bool isAsc) : base(streamMergeContext)
    {
        _dataSourceSequenceMatchOrderConfig = dataSourceSequenceMatchOrderConfig;
        _tableSequenceMatchOrderConfig = tableSequenceMatchOrderConfig;
        _routeQueryResults = routeQueryResults;
        _isAsc = isAsc;
    }

    protected override IEnumerable<ISqlRouteUnit> GetDefaultSqlRouteUnits()
    {
        var skip = GetStreamMergeContext().Skip.GetValueOrDefault();
        if (skip < 0)
            throw new ShardingCoreException("skip must ge 0");

        var take = GetStreamMergeContext().Take;
        if (take.HasValue && take.Value <= 0)
            throw new ShardingCoreException("take must gt 0");
        //分库是主要排序
        var dataSourceOrderMain = _dataSourceSequenceMatchOrderConfig != null;
        var sortRouteResults = _routeQueryResults.Select(o => new
        {
            DataSourceName = o.DataSourceName,
            Tail = o.TableRouteResult.ReplaceTables.First().Tail,
            RouteQueryResult = o
        });
        if (dataSourceOrderMain)
        {
            //是否有两级排序
            var useThenBy = _tableSequenceMatchOrderConfig != null;
            if (_isAsc)
            {
                sortRouteResults = sortRouteResults.OrderBy(o => o.DataSourceName,
                    _dataSourceSequenceMatchOrderConfig.RouteComparer).ThenByIf(o => o.Tail, useThenBy, _tableSequenceMatchOrderConfig.RouteComparer);
            }
            else
            {
                sortRouteResults = sortRouteResults.OrderByDescending(o => o.DataSourceName,
                    _dataSourceSequenceMatchOrderConfig.RouteComparer).ThenByDescendingIf(o => o.Tail, useThenBy, _tableSequenceMatchOrderConfig.RouteComparer);
            }
        }
        else
        {
            if (_isAsc)
            {
                sortRouteResults =
                    sortRouteResults.OrderBy(o => o.Tail, _tableSequenceMatchOrderConfig.RouteComparer);
            }
            else
            {
                sortRouteResults =
                    sortRouteResults.OrderByDescending(o => o.Tail, _tableSequenceMatchOrderConfig.RouteComparer);
            }
        }


        var sequenceResults = new SequencePaginationList(sortRouteResults.Select(o => o.RouteQueryResult)).Skip(skip).Take(take).ToList();
        return  sequenceResults.Select(sequenceResult => new SqlSequenceRouteUnit(sequenceResult));

    }
    protected override IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async)
    {
        return new SequenceEnumerableExecutor<TEntity>(GetStreamMergeContext(), async);
    }
}
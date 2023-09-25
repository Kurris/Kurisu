using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.ShardingPage.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeContexts;
using Kurisu.EFSharding.Sharding.MergeEngines.Enumerables;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;
using Kurisu.EFSharding.Sharding.PaginationConfigurations;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.EnumeratorStreamMergeEngines;

internal class EnumeratorStreamMergeEngineFactory<TEntity>
{
    private readonly StreamMergeContext _streamMergeContext;
    private readonly IShardingPageManager _shardingPageManager;
    private readonly ITableRouteManager _tableRouteManager;
    private readonly IMetadataManager _entityMetadataManager;
    private readonly IDatasourceRouteManager _dataSourceRouteManager;

    private EnumeratorStreamMergeEngineFactory(StreamMergeContext streamMergeContext)
    {
        _streamMergeContext = streamMergeContext;
        _shardingPageManager = streamMergeContext.ShardingRuntimeContext.GetShardingPageManager();
        _tableRouteManager = streamMergeContext.ShardingRuntimeContext.GetTableRouteManager();
        _entityMetadataManager = streamMergeContext.ShardingRuntimeContext.GetMetadataManager();
        _dataSourceRouteManager = streamMergeContext.ShardingRuntimeContext.GetDatasourceRouteManager();
    }

    public static EnumeratorStreamMergeEngineFactory<TEntity> Create(StreamMergeContext streamMergeContext)
    {
        return new EnumeratorStreamMergeEngineFactory<TEntity>(streamMergeContext);
    }

    public IVirtualDatasourceRoute GetRoute(Type entityType)
    {
        return _dataSourceRouteManager.GetRoute(entityType);
    }

    public IStreamEnumerable<TEntity> GetStreamEnumerable()
    {
        if (_streamMergeContext.IsRouteNotMatch())
        {
            return new EmptyShardingEnumerable<TEntity>(_streamMergeContext);
        }
        // //本次查询没有跨库没有跨表就可以直接执行
        // if (!_streamMergeContext.IsMergeQuery())
        // {
        //     return new SingleQueryStreamEnumerable<TEntity>(_streamMergeContext);
        // }

        if (_streamMergeContext.UseUnionAllMerge())
        {
            return new DefaultShardingEnumerable<TEntity>(_streamMergeContext);
        }

        var queryMethodName = _streamMergeContext.MergeQueryCompilerContext.GetQueryMethodName();
        switch (queryMethodName)
        {
            case nameof(Enumerable.First):
            case nameof(Enumerable.FirstOrDefault):
                return new FirstOrDefaultShardingEnumerable<TEntity>(_streamMergeContext);
            case nameof(Enumerable.Single):
            case nameof(Enumerable.SingleOrDefault):
                return new SingleOrDefaultShardingEnumerable<TEntity>(_streamMergeContext);
            case nameof(Enumerable.Last):
            case nameof(Enumerable.LastOrDefault):
                return new LastOrDefaultShardingEnumerable<TEntity>(_streamMergeContext);
        }

        //未开启系统分表或者本次查询涉及多张分表
        if (_streamMergeContext.IsPaginationQuery() && _streamMergeContext.IsSingleShardingEntityQuery() && _shardingPageManager.Current != null)
        {
            //获取虚拟表判断是否启用了分页配置
            var shardingEntityType = _streamMergeContext.GetSingleShardingEntityType();
            if (shardingEntityType == null)
                throw new ShardingCoreException($"query not found sharding data source or sharding table entity");

            if (_streamMergeContext.Orders.IsEmpty())
            {
                //自动添加属性顺序排序
                //除了判断属性名还要判断所属关系
                var mergeEngine = DoNoOrderAppendEnumeratorStreamMergeEngine(shardingEntityType);
                if (mergeEngine != null)
                    return mergeEngine;
            }
            else
            {
                var mergeEngine = DoOrderSequencePaginationEnumeratorStreamMergeEngine(shardingEntityType);

                if (mergeEngine != null)
                    return mergeEngine;
            }
        }


        return new DefaultShardingEnumerable<TEntity>(_streamMergeContext);
    }

    private IStreamEnumerable<TEntity> DoNoOrderAppendEnumeratorStreamMergeEngine(Type shardingEntityType)
    {
        var isShardingDataSource = _entityMetadataManager.IsShardingDatasource(shardingEntityType);
        var isShardingTable = _entityMetadataManager.IsShardingTable(shardingEntityType);
        PaginationSequenceConfig dataSourceSequenceOrderConfig = null;
        PaginationSequenceConfig tableSequenceOrderConfig = null;
        if (isShardingDataSource)
        {
            var virtualDataSourceRoute = GetRoute(shardingEntityType);
            if (virtualDataSourceRoute.EnablePagination)
            {
                dataSourceSequenceOrderConfig = virtualDataSourceRoute.PaginationMetadata.PaginationConfigs.OrderByDescending(o => o.AppendOrder)
                    .FirstOrDefault(o => o.AppendIfOrderNone && typeof(TEntity).ContainPropertyName(o.PropertyName));
            }
        }

        if (isShardingTable)
        {
            var tableRoute = _tableRouteManager.GetRoute(shardingEntityType);
            if (tableRoute.EnablePagination)
            {
                tableSequenceOrderConfig = tableRoute.PaginationMetadata.PaginationConfigs.OrderByDescending(o => o.AppendOrder)
                    .FirstOrDefault(o => o.AppendIfOrderNone && typeof(TEntity).ContainPropertyName(o.PropertyName));
            }
        }

        var useSequenceEnumeratorMergeEngine = isShardingDataSource && (dataSourceSequenceOrderConfig != null ||
                                                                        (isShardingTable &&
                                                                         !_streamMergeContext.IsCrossDataSource)) || (!isShardingDataSource && isShardingTable && tableSequenceOrderConfig != null);

        if (useSequenceEnumeratorMergeEngine)
        {
            return new AppendOrderSequenceShardingEnumerable<TEntity>(_streamMergeContext, dataSourceSequenceOrderConfig, tableSequenceOrderConfig, _shardingPageManager.Current.RouteQueryResults);
        }


        return null;
    }

    private IStreamEnumerable<TEntity> DoOrderSequencePaginationEnumeratorStreamMergeEngine(Type shardingEntityType)
    {
        var orderCount = _streamMergeContext.Orders.Count();
        var primaryOrder = _streamMergeContext.Orders.First();
        var isShardingDataSource = _entityMetadataManager.IsShardingDatasource(shardingEntityType);
        var isShardingTable = _entityMetadataManager.IsShardingTable(shardingEntityType);
        PaginationSequenceConfig dataSourceSequenceOrderConfig = null;
        PaginationSequenceConfig tableSequenceOrderConfig = null;
        IVirtualDatasourceRoute virtualDataSourceRoute = null;
        IVirtualTableRoute tableRoute = null;
        bool dataSourceUseReverse = true;
        bool tableUseReverse = true;
        if (isShardingDataSource)
        {
            virtualDataSourceRoute = GetRoute(shardingEntityType);
            if (virtualDataSourceRoute.EnablePagination)
            {
                dataSourceSequenceOrderConfig = orderCount == 1 ? GetPaginationFullMatch(virtualDataSourceRoute.PaginationMetadata.PaginationConfigs, primaryOrder) : GetPaginationPrimaryMatch(virtualDataSourceRoute.PaginationMetadata.PaginationConfigs, primaryOrder);
            }
        }

        if (isShardingTable)
        {
            tableRoute = _tableRouteManager.GetRoute(shardingEntityType);
            if (tableRoute.EnablePagination)
            {
                tableSequenceOrderConfig = orderCount == 1 ? GetPaginationFullMatch(tableRoute.PaginationMetadata.PaginationConfigs, primaryOrder) : GetPaginationPrimaryMatch(tableRoute.PaginationMetadata.PaginationConfigs, primaryOrder);
            }
        }

        var useSequenceEnumeratorMergeEngine = isShardingDataSource && (dataSourceSequenceOrderConfig != null ||
                                                                        (isShardingTable &&
                                                                         !_streamMergeContext.IsCrossDataSource)) || (!isShardingDataSource && isShardingTable && tableSequenceOrderConfig != null);
        if (useSequenceEnumeratorMergeEngine)
        {
            return new SequenceShardingEnumerable<TEntity>(_streamMergeContext, dataSourceSequenceOrderConfig, tableSequenceOrderConfig, _shardingPageManager.Current.RouteQueryResults, primaryOrder.IsAsc);
        }

        var total = _shardingPageManager.Current.RouteQueryResults.Sum(o => o.QueryResult);
        if (isShardingDataSource)
        {
            dataSourceUseReverse =
                virtualDataSourceRoute.EnablePagination && EntityDataSourceUseReverseShardingPage(virtualDataSourceRoute, total);
        }

        if (isShardingTable)
        {
            tableUseReverse =
                tableRoute.EnablePagination && EntityTableReverseShardingPage(tableRoute, total);
        }


        //skip过大reserve skip
        if (dataSourceUseReverse && tableUseReverse)
        {
            return new ReverseShardingEnumerable<TEntity>(_streamMergeContext, total);
        }


        return null;
    }

    private bool EntityDataSourceUseReverseShardingPage(IVirtualDatasourceRoute virtualDataSourceRoute, long total)
    {
        if (virtualDataSourceRoute.PaginationMetadata.EnableReverseShardingPage && _streamMergeContext.Take.GetValueOrDefault() > 0)
        {
            if (virtualDataSourceRoute.PaginationMetadata.IsUseReverse(_streamMergeContext.Skip.GetValueOrDefault(), total))
            {
                return true;
            }
        }

        return false;
    }

    private bool EntityTableReverseShardingPage(IVirtualTableRoute tableRoute, long total)
    {
        if (tableRoute.PaginationMetadata.EnableReverseShardingPage && _streamMergeContext.Take.GetValueOrDefault() > 0)
        {
            if (tableRoute.PaginationMetadata.IsUseReverse(_streamMergeContext.Skip.GetValueOrDefault(), total))
            {
                return true;
            }
        }

        return false;
    }

    private PaginationSequenceConfig GetPaginationFullMatch(ISet<PaginationSequenceConfig> paginationSequenceConfigs, PropertyOrder primaryOrder)
    {
        return paginationSequenceConfigs.FirstOrDefault(o => PaginationPrimaryMatch(o, primaryOrder));
    }

    private PaginationSequenceConfig GetPaginationPrimaryMatch(ISet<PaginationSequenceConfig> paginationSequenceConfigs, PropertyOrder primaryOrder)
    {
        return paginationSequenceConfigs.Where(o => o.PaginationMatchEnum.HasFlag(PaginationMatchEnum.PrimaryMatch)).FirstOrDefault(o => PaginationPrimaryMatch(o, primaryOrder));
    }

    private bool PaginationPrimaryMatch(PaginationSequenceConfig paginationSequenceConfig, PropertyOrder propertyOrder)
    {
        if (propertyOrder.PropertyExpression != paginationSequenceConfig.PropertyName)
            return false;

        if (paginationSequenceConfig.PaginationMatchEnum.HasFlag(PaginationMatchEnum.Owner))
            return _streamMergeContext.GetSingleShardingEntityType() == paginationSequenceConfig.OrderPropertyInfo.DeclaringType;
        if (paginationSequenceConfig.PaginationMatchEnum.HasFlag(PaginationMatchEnum.Named))
            return propertyOrder.PropertyExpression == paginationSequenceConfig.PropertyName;
        return false;
    }
}
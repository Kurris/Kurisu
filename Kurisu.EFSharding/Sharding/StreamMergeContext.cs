using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.MergeContexts;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingComparision.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding;

internal class StreamMergeContext : IMergeParseContext, IDisposable, IAsyncDisposable
{
    public IMergeQueryCompilerContext MergeQueryCompilerContext { get; }
    public IShardingRuntimeContext ShardingRuntimeContext { get; }
    public IParseResult ParseResult { get; }
    public IQueryable RewriteQueryable { get; }
    public IOptimizeResult OptimizeResult { get; }

    private readonly IRewriteResult _rewriteResult;
    private readonly IRouteTailFactory _routeTailFactory;

    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public PropertyOrder[] Orders { get; private set; }

    public SelectContext SelectContext => ParseResult.GetSelectContext();
    public GroupByContext GroupByContext => ParseResult.GetGroupByContext();
    public ShardingRouteResult ShardingRouteResult => MergeQueryCompilerContext.GetShardingRouteResult();

    /// <summary>
    /// 本次查询涉及的对象
    /// </summary>
    public ISet<Type> QueryEntities { get; }


    /// <summary>
    /// 本次查询跨库
    /// </summary>
    public bool IsCrossDataSource => MergeQueryCompilerContext.IsCrossDataSource();

    /// <summary>
    /// 本次查询跨表
    /// </summary>
    public bool IsCrossTable => MergeQueryCompilerContext.IsCrossTable();

    private readonly ITrackerManager _trackerManager;

    private readonly ConcurrentDictionary<DbContext, object> _parallelDbContexts;

    public IComparer<string> ShardingTailComparer => OptimizeResult.ShardingTailComparer();

    /// <summary>
    /// 分表后缀比较是否重排正序
    /// </summary>
    public bool TailComparerNeedReverse => OptimizeResult.SameWithTailComparer();


    public StreamMergeContext(IMergeQueryCompilerContext mergeQueryCompilerContext, IParseResult parseResult,
        IRewriteResult rewriteResult, IOptimizeResult optimizeResult)
    {
        MergeQueryCompilerContext = mergeQueryCompilerContext;
        ParseResult = parseResult;
        RewriteQueryable = rewriteResult.GetRewriteQueryable();
        OptimizeResult = optimizeResult;
        _rewriteResult = rewriteResult;
        ShardingRuntimeContext = ((DbContext) mergeQueryCompilerContext.GetShardingDbContext()).GetShardingRuntimeContext();
        _routeTailFactory = ShardingRuntimeContext.GetRouteTailFactory();
        _trackerManager = ShardingRuntimeContext.GetTrackerManager();
        QueryEntities = MergeQueryCompilerContext.GetQueryEntities().Keys.ToHashSet();
        _parallelDbContexts = new ConcurrentDictionary<DbContext, object>(Environment.ProcessorCount, mergeQueryCompilerContext.GetShardingRouteResult().RouteUnits.Count);
        Orders = parseResult.GetOrderByContext().PropertyOrders.ToArray();
        Skip = parseResult.GetPaginationContext().Skip;
        Take = parseResult.GetPaginationContext().Take;
    }

    public void ReSetOrders(PropertyOrder[] orders)
    {
        Orders = orders;
    }

    public void ReSetSkip(int? skip)
    {
        Skip = skip;
    }

    public void ReSetTake(int? take)
    {
        Take = take;
    }

    /// <summary>
    /// 创建对应的dbcontext
    /// </summary>
    /// <param name="sqlRouteUnit">数据库路由最小单元</param>
    /// <returns></returns>
    public DbContext CreateDbContext(ISqlRouteUnit sqlRouteUnit)
    {
        var routeTail = _routeTailFactory.Create(sqlRouteUnit.TableRouteResult);

        var dbContext = GetShardingDbContext().GetShardingExecutor().CreateDbContext(CreateDbContextStrategyEnum.IndependentConnectionQuery, sqlRouteUnit.DatasourceName, routeTail);
        _parallelDbContexts.TryAdd(dbContext, null);

        return dbContext;
    }

    // /// <summary>
    // /// 因为并发查询情况下那么你是内存就是内存你是流式就是流式
    // /// 如果不是并发查询的情况下系统会将当前dbcontext进行利用起来所以只能是流式
    // /// </summary>
    // /// <param name="connectionMode"></param>
    // /// <returns></returns>
    // public ConnectionModeEnum RealConnectionMode(ConnectionModeEnum connectionMode)
    // {
    //     if (IsParallelQuery())
    //     {
    //         return connectionMode;
    //     }
    //     else
    //     {
    //         return ConnectionModeEnum.MEMORY_STRICTLY;
    //     }
    // }

    //public IRouteTail Create(TableRouteResult tableRouteResult)
    //{
    //    return _routeTailFactory.Create(tableRouteResult);
    //}

    public IQueryable GetReWriteQueryable()
    {
        return RewriteQueryable;
    }

    public IQueryable GetOriginalQueryable()
    {
        return MergeQueryCompilerContext.GetQueryCombineResult().GetCombineQueryable();
    }

    public int? GetPaginationReWriteTake()
    {
        if (Take.HasValue)
            return Skip.GetValueOrDefault() + Take.Value;
        return default;
    }
    //public bool HasSkipTake()
    //{
    //    return Skip.HasValue || Take.HasValue;
    //}

    /// <summary>
    /// 任意skip或者take大于0那么就说明是分页的查询
    /// </summary>
    /// <returns></returns>
    public bool IsPaginationQuery()
    {
        return Skip is > 0 || Take is > 0;
    }


    public bool HasGroupQuery()
    {
        return this.GroupByContext.GroupExpression != null;
    }

    /// <summary>
    /// group 内存排序
    /// </summary>
    /// <returns></returns>
    public bool GroupQueryMemoryMerge()
    {
        return HasGroupQuery() && this.GroupByContext.GroupMemoryMerge;
    }

    public bool IsMergeQuery()
    {
        return IsCrossDataSource || IsCrossTable;
    }

    public bool IsSingleShardingEntityQuery()
    {
        return QueryEntities.Where(o => MergeQueryCompilerContext.GetEntityMetadataManager().IsSharding(o)).Take(2)
            .Count() == 1;
    }

    public Type GetSingleShardingEntityType()
    {
        return QueryEntities.Single(o => MergeQueryCompilerContext.GetEntityMetadataManager().IsSharding(o));
    }
    //public bool HasAggregateQuery()
    //{
    //    return this.SelectContext.HasAverage();
    //}

    public IShardingDbContext GetShardingDbContext()
    {
        return MergeQueryCompilerContext.GetShardingDbContext();
    }

    public int GetMaxQueryConnectionsLimit()
    {
        return OptimizeResult.GetMaxQueryConnectionsLimit();
    }

    /// <summary>
    /// 是否启用读写分离
    /// </summary>
    /// <returns></returns>
    private bool IsUseReadWriteSeparation()
    {
        // return GetShardingDbContext().IsUseReadWriteSeparation() &&
        //        GetShardingDbContext();

        return false;
    }

    /// <summary>
    /// 是否使用并行查询仅分库无所谓可以将分库的当前DbContext进行储存起来但是分表就不行因为一个DbContext最多一对一表
    /// </summary>
    /// <returns></returns>
    public bool IsParallelQuery()
    {
        return MergeQueryCompilerContext.IsParallelQuery();
    }

    /// <summary>
    /// 是否使用sharding track
    /// </summary>
    /// <returns></returns>
    public bool IsUseShardingTrack(Type entityType)
    {
        if (!IsParallelQuery())
        {
            return false;
        }

        return QueryTrack() && _trackerManager.EntityUseTrack(entityType);
    }

    private bool QueryTrack()
    {
        return MergeQueryCompilerContext.IsQueryTrack();
    }

    public IShardingComparer GetShardingComparer()
    {
        return ((DbContext) GetShardingDbContext()).GetShardingRuntimeContext().GetShardingComparer();
    }

    /// <summary>
    /// 如果返回false那么就说明不需要继续查询了
    /// 返回true表示需要继续查询
    /// </summary>
    /// <param name="emptyFunc"></param>
    /// <param name="r"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    /// <exception cref="ShardingCoreQueryRouteNotMatchException"></exception>
    public bool TryPrepareExecuteContinueQuery<TResult>(Func<TResult> emptyFunc, out TResult r)
    {
        if (TakeZeroNoQueryExecute())
        {
            r = emptyFunc();
            return false;
        }

        if (IsRouteNotMatch())
        {
            if (ThrowIfQueryRouteNotMatch())
            {
                throw new ShardingCoreQueryRouteNotMatchException(MergeQueryCompilerContext.GetQueryExpression()
                    .ShardingPrint());
            }

            r = emptyFunc();
            return false;
        }

        r = default;
        return true;
    }

    /// <summary>
    /// 无路由匹配
    /// </summary>
    /// <returns></returns>
    public bool IsRouteNotMatch()
    {
        return ShardingRouteResult.IsEmpty;
    }

    /// <summary>
    /// take有值并且是0的情况下那么就说明不需要获取
    /// </summary>
    /// <returns></returns>
    public bool TakeZeroNoQueryExecute()
    {
        return Take is 0;
    }

    private bool ThrowIfQueryRouteNotMatch()
    {
        return false;
    }

    public bool UseUnionAllMerge()
    {
        return MergeQueryCompilerContext.UseUnionAllMerge();
    }

    public void Dispose()
    {
        foreach (var dbContext in _parallelDbContexts.Keys)
        {
            dbContext.Dispose();
        }
    }


    public async ValueTask DisposeAsync()
    {
        foreach (var dbContext in _parallelDbContexts.Keys)
        {
            await dbContext.DisposeAsync();
        }
    }

    public bool IsSeqQuery()
    {
        return OptimizeResult.IsSequenceQuery();
    }

    public int? GetSkip()
    {
        return Skip;
    }

    public int? GetTake()
    {
        return Take;
    }

    public void ReverseOrder()
    {
        if (Orders.Any())
        {
            var propertyOrders = Orders.Select(o => new PropertyOrder(o.PropertyExpression, !o.IsAsc, o.OwnerType))
                .ToArray();
            ReSetOrders(propertyOrders);
        }
    }

    public async ValueTask<bool> DbContextDisposeAsync(DbContext dbContext)
    {
        if (_parallelDbContexts.TryRemove(dbContext, out _))
        {
            await dbContext.DisposeAsync();

            return true;
        }

        return false;
    }
}
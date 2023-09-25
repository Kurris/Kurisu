using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.InternalExtensions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingExecutors;

internal static class ShardingExecutor
{
    public static TResult Execute<TResult>(StreamMergeContext streamMergeContext,
        IExecutor<TResult> executor, bool async, IEnumerable<ISqlRouteUnit> sqlRouteUnits,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return ExecuteAsync(streamMergeContext, executor, async, sqlRouteUnits, cancellationToken).WaitAndUnwrapException(false);
    }

    public static async Task<TResult> ExecuteAsync<TResult>(StreamMergeContext streamMergeContext,
        IExecutor<TResult> executor, bool async, IEnumerable<ISqlRouteUnit> sqlRouteUnits,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var resultGroups = Execute0<TResult>(streamMergeContext, executor, async, sqlRouteUnits, cancellationToken).ToArray();

        var results = (await TaskHelper.WhenAllFastFail(resultGroups))
            .SelectMany(o => o)
            .ToList();

        if (results.IsEmpty())
            throw new ShardingCoreException("sharding execute result empty");
        //不需要merge
        //if (results.Count == 1)
        //{
        //    return results[0];
        //}
        var streamMerge = executor.GetShardingMerger().StreamMerge(results);
        return streamMerge;
    }

    private static Task<List<TResult>>[] Execute0<TResult>(StreamMergeContext streamMergeContext,
        IExecutor<TResult> executor, bool async, IEnumerable<ISqlRouteUnit> sqlRouteUnits,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var waitTaskQueue = ReOrderTableTails(streamMergeContext, sqlRouteUnits)
            .GroupBy(o => o.DatasourceName)
            .Select(o => GetSqlExecutorGroups(streamMergeContext, o))
            .Select(dataSourceSqlExecutorUnit =>
            {
                return Task.Run(async () =>
                {
                    if (streamMergeContext.UseUnionAllMerge())
                    {
                        var customerDatabaseSqlSupportManager =
                            streamMergeContext.ShardingRuntimeContext.GetUnionAllMergeManager();
                        using (customerDatabaseSqlSupportManager.CreateScope(
                                   ((UnSupportSqlRouteUnit) dataSourceSqlExecutorUnit.SqlExecutorGroups[0].Groups[0]
                                       .RouteUnit).TableRouteResults))
                        {
                            return await executor.ExecuteAsync(async, dataSourceSqlExecutorUnit,
                                cancellationToken);
                        }
                    }
                    else
                    {
                        return await executor.ExecuteAsync(async, dataSourceSqlExecutorUnit,
                            cancellationToken);
                    }
                }, cancellationToken);
            }).ToArray();
        return waitTaskQueue;
    }

    /// <summary>
    /// 顺序查询从重排序
    /// </summary>
    /// <param name="streamMergeContext"></param>
    /// <param name="sqlRouteUnits"></param>
    /// <returns></returns>
    private static IEnumerable<ISqlRouteUnit> ReOrderTableTails(StreamMergeContext streamMergeContext,
        IEnumerable<ISqlRouteUnit> sqlRouteUnits)
    {
        if (streamMergeContext.IsSeqQuery())
        {
            return sqlRouteUnits.OrderByAscDescIf(o => Enumerable.First<TableRouteUnit>(o.TableRouteResult.ReplaceTables).Tail,
                streamMergeContext.TailComparerNeedReverse, streamMergeContext.ShardingTailComparer);
        }

        return sqlRouteUnits;
    }

    /// <summary>
    /// 每个数据源下的分表结果按 maxQueryConnectionsLimit 进行组合分组每组大小 maxQueryConnectionsLimit
    /// ConnectionModeEnum为用户配置或者系统自动计算,哪怕是用户指定也是按照maxQueryConnectionsLimit来进行分组。
    /// </summary>
    /// <param name="streamMergeContext"></param>
    /// <param name="sqlGroups"></param>
    /// <returns></returns>
    private static DataSourceSqlExecutorUnit GetSqlExecutorGroups(StreamMergeContext streamMergeContext,
        IGrouping<string, ISqlRouteUnit> sqlGroups)
    {
        var maxQueryConnectionsLimit = streamMergeContext.GetMaxQueryConnectionsLimit();
        var sqlCount = sqlGroups.Count();
        ////根据用户配置单次查询期望并发数
        //int exceptCount =
        //    Math.Max(
        //        0 == sqlCount % maxQueryConnectionsLimit
        //            ? sqlCount / maxQueryConnectionsLimit
        //            : sqlCount / maxQueryConnectionsLimit + 1, 1);


        //将SqlExecutorUnit进行分区,每个区maxQueryConnectionsLimit个
        //[1,2,3,4,5,6,7],maxQueryConnectionsLimit=3,结果就是[[1,2,3],[4,5,6],[7]]
        var sqlExecutorUnitPartitions = sqlGroups.Select(o => new SqlExecutorUnit(o))
            .Chunk(maxQueryConnectionsLimit);

        var sqlExecutorGroups = sqlExecutorUnitPartitions
            .Select(o => new SqlExecutorGroup<SqlExecutorUnit>(o.ToList())).ToList();
        return new DataSourceSqlExecutorUnit(sqlExecutorGroups);
    }
}
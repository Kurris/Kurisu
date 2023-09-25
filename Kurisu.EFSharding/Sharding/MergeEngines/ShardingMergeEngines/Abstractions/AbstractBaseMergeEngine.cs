using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;


internal abstract class AbstractBaseMergeEngine
{
    private readonly StreamMergeContext _streamMergeContext;

    public AbstractBaseMergeEngine(StreamMergeContext streamMergeContext)
    {
        _streamMergeContext = streamMergeContext;
    }

    protected StreamMergeContext GetStreamMergeContext()
    {
        return _streamMergeContext;
    }
    /// <summary>
    /// sql执行的路由最小单元
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<ISqlRouteUnit> GetDefaultSqlRouteUnits()
    {
        if (_streamMergeContext.UseUnionAllMerge())
        {
            return _streamMergeContext.ShardingRouteResult.RouteUnits.GroupBy(o=>o.DatasourceName).Select(o=>new UnSupportSqlRouteUnit(o.Key,o.Select(g=>g.TableRouteResult).ToList()));
        }
        return _streamMergeContext.ShardingRouteResult.RouteUnits;

    }
}
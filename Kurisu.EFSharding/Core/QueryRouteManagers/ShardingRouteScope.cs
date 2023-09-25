using Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

namespace Kurisu.EFSharding.Core.QueryRouteManagers;

public class ShardingRouteScope : IDisposable
{

    /// <summary>
    /// 分表配置访问器
    /// </summary>
    public IShardingRouteAccessor ShardingRouteAccessor { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="shardingRouteAccessor"></param>
    public ShardingRouteScope(IShardingRouteAccessor shardingRouteAccessor)
    {
        ShardingRouteAccessor = shardingRouteAccessor;
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Dispose()
    {
        ShardingRouteAccessor.ShardingRouteContext = null;
    }
}
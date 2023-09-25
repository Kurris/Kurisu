namespace Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

public interface IShardingRouteManager
{
    ShardingRouteContext Current { get; }

    /// <summary>
    /// 创建路由scope
    /// </summary>
    /// <returns></returns>
    ShardingRouteScope CreateScope();
}
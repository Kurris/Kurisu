using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingDbContext
{
    /// <summary>
    /// 获取分片执行者
    /// </summary>
    /// <returns></returns>
    IShardingDbContextExecutor GetShardingExecutor();


    IRouteTail RouteTail { get; set; }
}
namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public enum ReadStrategyEnum
{
    Random=1,
    Loop=2,
}

public enum ReadConnStringGetStrategyEnum
{
    /// <summary>
    /// 每次都是最新的
    /// </summary>
    LatestEveryTime,
    /// <summary>
    /// 已dbcontext作为缓存条件每次都是第一次获取的
    /// </summary>
    LatestFirstTime
}
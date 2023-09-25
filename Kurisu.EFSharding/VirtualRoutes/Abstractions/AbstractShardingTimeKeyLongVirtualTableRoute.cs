using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Helpers;

namespace Kurisu.EFSharding.VirtualRoutes.Abstractions;

/// <summary>
/// sharding table route by time stamp (ms)
/// </summary>
/// <typeparam name="TEntity">entity</typeparam>
public abstract class AbstractShardingTimeKeyLongVirtualTableRoute<TEntity> : AbstractShardingAutoCreateOperatorVirtualTableRoute<TEntity, long>
    where TEntity : class, new()
{
    protected AbstractShardingTimeKeyLongVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    /// <summary>
    /// how convert sharding key to tail
    /// </summary>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    public override string ToTail(object shardingKey)
    {
        var time = (long) shardingKey;
        return TimeFormatToTail(time);
    }

    /// <summary>
    /// how format long time to tail
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected abstract string TimeFormatToTail(long time);

    protected override string ConvertNowToTail(DateTime now)
    {
        return ToTail(ShardingCoreHelper.ConvertDateTimeToLong(now));
    }
}
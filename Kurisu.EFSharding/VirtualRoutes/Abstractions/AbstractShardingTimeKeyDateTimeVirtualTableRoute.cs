using Kurisu.EFSharding.Core.DependencyInjection;

namespace Kurisu.EFSharding.VirtualRoutes.Abstractions;

/// <summary>
/// time type is date time
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class AbstractShardingTimeKeyDateTimeVirtualTableRoute<TEntity> : AbstractShardingAutoCreateOperatorVirtualTableRoute<TEntity, DateTime>
    where TEntity : class, new()
{
    protected AbstractShardingTimeKeyDateTimeVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    /// <summary>
    /// how convert sharding key to tail
    /// </summary>
    /// <param name="shardingKey"></param>
    /// <returns></returns>
    public override string ToTail(object shardingKey)
    {
        var time = Convert.ToDateTime(shardingKey);
        return TimeFormatToTail(time);
    }

    /// <summary>
    /// how format date time to tail
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected abstract string TimeFormatToTail(DateTime time);

    protected override string ConvertNowToTail(DateTime now)
    {
        return ToTail(now);
    }
}
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.VirtualRoutes.Abstractions;

namespace Kurisu.EFSharding.VirtualRoutes.Days;

public abstract class AbstractSimpleShardingDayKeyLongVirtualTableRoute<TEntity> : AbstractShardingTimeKeyLongVirtualTableRoute<TEntity>
    where TEntity : class, new()
{
    protected abstract DateTime GetBeginTime();

    protected override List<string> CalcTailsOnStart()
    {
        var beginTime = GetBeginTime().Date;

        var tails = new List<string>();
        //提前创建表
        var nowTimeStamp = DateTime.Now.Date;
        if (beginTime > nowTimeStamp)
            throw new ArgumentException("begin time error");
        var currentTimeStamp = beginTime;
        while (currentTimeStamp <= nowTimeStamp)
        {
            var currentTimeStampLong = ShardingCoreHelper.ConvertDateTimeToLong(currentTimeStamp);
            var tail = TimeFormatToTail(currentTimeStampLong);
            tails.Add(tail);
            currentTimeStamp = currentTimeStamp.AddDays(1);
        }

        return tails;
    }

    protected override string TimeFormatToTail(long time)
    {
        var dateTime = ShardingCoreHelper.ConvertLongToDateTime(time);
        return $"{dateTime:yyyyMMdd}";
    }

    protected override Func<string, bool> GetRouteToFilter(long shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var t = TimeFormatToTail(shardingKey);
        switch (shardingOperator)
        {
            case ShardingOperatorEnum.GreaterThan:
            case ShardingOperatorEnum.GreaterThanOrEqual:
                return tail => String.Compare(tail, t, StringComparison.Ordinal) >= 0;
            case ShardingOperatorEnum.LessThan:
            {
                var dateTime = ShardingCoreHelper.ConvertLongToDateTime(shardingKey);
                var shardingKeyDate = dateTime.Date;
                //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                if (shardingKeyDate == dateTime)
                    return tail => string.Compare(tail, t, StringComparison.Ordinal) < 0;
                return tail => string.Compare(tail, t, StringComparison.Ordinal) <= 0;
            }
            case ShardingOperatorEnum.LessThanOrEqual:
                return tail => string.Compare(tail, t, StringComparison.Ordinal) <= 0;
            case ShardingOperatorEnum.Equal: return tail => tail == t;
            case ShardingOperatorEnum.UnKnown:
            case ShardingOperatorEnum.NotEqual:
            case ShardingOperatorEnum.AllLike:
            case ShardingOperatorEnum.StartLike:
            case ShardingOperatorEnum.EndLike:
            default:
            {
                return _ => true;
            }
        }
    }

    protected AbstractSimpleShardingDayKeyLongVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }
}
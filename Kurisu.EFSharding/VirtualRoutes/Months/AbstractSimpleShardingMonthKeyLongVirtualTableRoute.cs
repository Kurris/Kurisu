using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.VirtualRoutes.Abstractions;

namespace Kurisu.EFSharding.VirtualRoutes.Months;

public abstract class AbstractSimpleShardingMonthKeyLongVirtualTableRoute<TEntity> : AbstractShardingTimeKeyLongVirtualTableRoute<TEntity>
    where TEntity : class, new()
{
    public abstract DateTime GetBeginTime();

    protected override List<string> CalcTailsOnStart()
    {
        var beginTime = ShardingCoreHelper.GetCurrentMonthFirstDay(GetBeginTime());

        var tails = new List<string>();
        //提前创建表
        var nowTimeStamp = ShardingCoreHelper.GetCurrentMonthFirstDay(DateTime.Now);
        if (beginTime > nowTimeStamp)
            throw new ArgumentException("begin time error");
        var currentTimeStamp = beginTime;
        while (currentTimeStamp <= nowTimeStamp)
        {
            var currentTimeStampLong = ShardingCoreHelper.ConvertDateTimeToLong(currentTimeStamp);
            var tail = ToTail(currentTimeStampLong);
            tails.Add(tail);
            currentTimeStamp = ShardingCoreHelper.GetNextMonthFirstDay(currentTimeStamp);
        }

        return tails;
    }

    protected override string TimeFormatToTail(long time)
    {
        var datetime = ShardingCoreHelper.ConvertLongToDateTime(time);
        return $"{datetime:yyyyMM}";
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
                var currentMonth = ShardingCoreHelper.GetCurrentMonthFirstDay(dateTime);
                //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                if (currentMonth == dateTime)
                    return tail => String.Compare(tail, t, StringComparison.Ordinal) < 0;
                return tail => String.Compare(tail, t, StringComparison.Ordinal) <= 0;
            }
            case ShardingOperatorEnum.LessThanOrEqual:
                return tail => String.Compare(tail, t, StringComparison.Ordinal) <= 0;
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

    protected AbstractSimpleShardingMonthKeyLongVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }
}
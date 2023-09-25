using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.VirtualRoutes.Abstractions;

namespace Kurisu.EFSharding.VirtualRoutes.Months;

public abstract class AbstractSimpleShardingMonthKeyDateTimeVirtualTableRoute<TEntity> : AbstractShardingTimeKeyDateTimeVirtualTableRoute<TEntity>
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
            var tail = ToTail(currentTimeStamp);
            tails.Add(tail);
            currentTimeStamp = ShardingCoreHelper.GetNextMonthFirstDay(currentTimeStamp);
        }

        return tails;
    }

    protected override string TimeFormatToTail(DateTime time)
    {
        return $"{time:yyyyMM}";
    }

    protected override Func<string, bool> GetRouteToFilter(DateTime shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var t = TimeFormatToTail(shardingKey);
        switch (shardingOperator)
        {
            case ShardingOperatorEnum.GreaterThan:
            case ShardingOperatorEnum.GreaterThanOrEqual:
                return tail => String.Compare(tail, t, StringComparison.Ordinal) >= 0;
            case ShardingOperatorEnum.LessThan:
            {
                var currentMonth = ShardingCoreHelper.GetCurrentMonthFirstDay(shardingKey);
                //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                if (currentMonth == shardingKey)
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

    protected AbstractSimpleShardingMonthKeyDateTimeVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }
}
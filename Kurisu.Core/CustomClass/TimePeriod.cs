﻿namespace Kurisu.Core.CustomClass;

/// <summary>
/// 时分段
/// </summary>
public class TimePeriod
{
    /// <summary>
    /// ctor
    /// </summary>
    public TimePeriod()
    {

    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public TimePeriod(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// 开始时间
    /// </summary>
    public TimeOnly? Start { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public TimeOnly? End { get; set; }

    /// <summary>
    /// 判断跨天
    /// </summary>
    /// <returns></returns>
    public bool IsCrossDay()
    {
        return End < Start;
    }

    /// <summary>
    /// 判断是否重复
    /// </summary>
    /// <returns></returns>
    public bool IsDuplicate(TimePeriod period)
    {
        return !Check();

        bool Check()
        {
            if (IsCrossDay())
            {
                if (period.IsCrossDay())
                {
                    return true;
                }
                else
                {
                    return period.Start > End && period.End < Start;
                }
            }
            else
            {
                if (period.IsCrossDay())
                {
                    return period.Start > End && period.End < Start;
                }
                else
                {
                    return period.Start > End || period.End < Start;
                }
            }
        }
    }

    /// <summary>
    /// 当前时间在区间内
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public bool IsPresent(DateTime dateTime)
    {
        DateTime start;
        DateTime end;

        if (IsCrossDay())
        {
            //period = [22:00:00,03:00:00]
            var currentStart = DateTime.Today.Add(Start.Value.ToTimeSpan());
            var endOfDay = DateTime.Today.AddDays(1).AddSeconds(-1);

            //assume is left range . then currentStart is 2024-04-16 22:00:00 and endOfDay is 2024-04-16 23:59:59
            if (dateTime >= currentStart && dateTime <= endOfDay)
            {
                start = currentStart; //2024-04-16 22:00:00
                end = DateTime.Today.AddDays(1).Add(End.Value.ToTimeSpan());//2024-04-17 03:00:00
            }
            else
            {

                start = DateTime.Today.AddDays(-1).Add(Start.Value.ToTimeSpan());
                end = DateTime.Today.Add(End.Value.ToTimeSpan());
            }
        }
        else
        {
            start = DateTime.Today.Add(Start.Value.ToTimeSpan());
            end = DateTime.Today.Add(End.Value.ToTimeSpan());
        }

        return dateTime >= start && dateTime <= end;
    }
}
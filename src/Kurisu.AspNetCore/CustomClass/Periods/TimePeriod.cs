using System;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 时分段
/// </summary>
public struct TimePeriod : IDateTimeComparable<TimeOnly, TimePeriod>
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

    /// <inheritdoc />
    public TimeOnly? Start { get; set; }

    /// <inheritdoc />
    public TimeOnly? End { get; set; }
    
    
    /// <inheritdoc />
    public bool HasValue => Start.HasValue && End.HasValue;

    /// <inheritdoc />
    public bool IsCrossDay => End < Start;

    /// <inheritdoc />
    public bool IsPresent(TimeOnly value)
    {
        var s = Start!.Value;
        var e = End!.Value;
        if (IsCrossDay)
        {
            return value >= s || value <= e;
        }

        return value >= s && value <= e;
    }

    /// <summary>
    /// 判断是否重合
    /// </summary>
    /// <returns></returns>
    public bool IsOverlap(TimePeriod period)
    {
        return !Check(period);
    }

    private bool Check(TimePeriod period)
    {
        if (IsCrossDay)
        {
            if (period.IsCrossDay)
            {
                return true;
            }

            return period.Start > End && period.End < Start;
        }

        if (period.IsCrossDay)
        {
            return period.Start > End && period.End < Start;
        }

        return period.Start > End || period.End < Start;
    }

    /// <summary>
    /// 当前时间在区间内
    /// </summary>
    /// <param name="dateTime">当前时间</param>
    /// <returns></returns>
    public bool IsPresent(DateTime dateTime)
    {
        DateTime start;
        DateTime end;

        if (IsCrossDay)
        {
            //period = [22:00:00,03:00:00]
            var currentStart = dateTime.Date.Add(Start!.Value.ToTimeSpan());
            var endOfDay = dateTime.Date.AddDays(1).AddSeconds(-1);

            //assume is left range . then currentStart is 2024-04-16 22:00:00 and endOfDay is 2024-04-16 23:59:59
            if (dateTime >= currentStart && dateTime <= endOfDay)
            {
                start = currentStart; //2024-04-16 22:00:00
                end = dateTime.Date.AddDays(1).Add(End!.Value.ToTimeSpan()); //2024-04-17 03:00:00
            }
            else
            {
                start = dateTime.Date.AddDays(-1).Add(Start!.Value.ToTimeSpan());
                end = dateTime.Date.Add(End!.Value.ToTimeSpan());
            }
        }
        else
        {
            start = dateTime.Date.Add(Start!.Value.ToTimeSpan());
            end = dateTime.Date.Add(End!.Value.ToTimeSpan());
        }

        return dateTime >= start && dateTime <= end;
    }
}
namespace Kurisu.Core.CustomClass;

/// <summary>
/// 时分段
/// </summary>
public class TimePeriod
{
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
        var currentHours = GetHourPeriod();
        var hours = period.GetHourPeriod();
        if (hours.Concat(currentHours).Distinct().Count() != currentHours.Count + hours.Count)
        {
            if (IsCrossDay() && period.IsCrossDay())
            {
                return true;
            }

            return !(period.Start > End && period.End < Start);
        }

        return false;
    }

    /// <summary>
    /// 当前时间在区间内
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public bool IsPresent(DateTime? dateTime)
    {
        dateTime ??= DateTime.Now;
        
        var nowHour = dateTime.Value.Hour;
        var nowMin = dateTime.Value.Minute;
        
        var hours = GetHourPeriod();
        
        if (!hours.Contains(nowHour))
        {
            return false;
        }

        if (nowHour == Start.Value.Hour)
            return nowMin > Start.Value.Minute;
        else if (nowHour == End.Value.Hour)
            return nowMin < End.Value.Minute;

        return true;
    }

    private IReadOnlyList<int> GetHourPeriod()
    {
        var hours = new List<int>();
        if (IsCrossDay())
        {
            for (int i = 0; i < (25 - Start.Value.Hour); i++)
                hours.Add(Start.Value.Hour + i);

            for (int i = 0; i < End.Value.Hour; i++)
                hours.Add(i + 1);
        }
        else
        {
            for (int i = Start.Value.Hour; i < End.Value.Hour + 1; i++)
                hours.Add(i);
        }

        return hours;
    }
}
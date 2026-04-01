using Kurisu.AspNetCore.Abstractions.Utils.Periods.Abstractions;

namespace Kurisu.AspNetCore.Abstractions.Utils.Periods;

/// <summary>
/// 表示一个日期时间区间。
/// </summary>
public class DateTimePeriod : IPeriod<DateTime>, IPeriodComparable<DateTime, DateTimePeriod>
{
    /// <summary>
    /// 初始化 <see cref="DateTimePeriod"/> 类的新实例。 
    /// </summary>
    public DateTimePeriod()
    {
        
    }

    /// <summary>
    /// 使用起始和结束时间字符串初始化 <see cref="DateTimePeriod"/> 类的新实例。
    /// </summary>
    /// <param name="start">起始时间字符串。</param>
    /// <param name="end">结束时间字符串。</param>
    public DateTimePeriod(string start, string end)
        : this(DateTime.Parse(start), DateTime.Parse(end))
    {
    }

    /// <summary>
    /// 使用起始和结束时间初始化 <see cref="DateTimePeriod"/> 类的新实例。
    /// </summary>
    /// <param name="start">起始时间。</param>
    /// <param name="end">结束时间。</param>
    public DateTimePeriod(DateTime start, DateTime end)
    {
        Start = start;
        End = end;

        Validate();
    }

    /// <summary>
    /// 获取或设置区间的起始时间。
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    /// 获取或设置区间的结束时间。
    /// </summary>
    public DateTime End { get; init; }

    /// <inheritdoc />
    public bool IsPresent(DateTime value)
    {
        return value >= Start && value <= End;
    }

    /// <inheritdoc />
    public bool IsCrossDay => Start.Date != End.Date;

    /// <inheritdoc />
    public bool IsOverlap(DateTimePeriod period)
    {
        return !(period.Start > End || period.End < Start);
    }


    public void Validate()
    {
        if (Start > End)
        {
            throw new ArgumentException("开始时间不能大于结束时间.", nameof(Start));
        }
    }
}
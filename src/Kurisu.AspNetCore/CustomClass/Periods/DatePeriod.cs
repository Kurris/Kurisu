using System;
using System.Text.Json.Serialization;
using Kurisu.AspNetCore.CustomClass.Periods.Abstractions;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 表示一个日期区间。
/// </summary>
public class DatePeriod : IPeriod<DateOnly>, IPeriodComparable<DateOnly, DatePeriod>
{
    /// <summary>
    /// 使用起始日期字符串初始化 <see cref="DatePeriod"/> 类的新实例。
    /// </summary>
    /// <param name="start">起始日期字符串。</param>
    /// <param name="end">结束日期字符串。</param>
    public DatePeriod(string start, string end)
        : this(DateOnly.Parse(start), DateOnly.Parse(end))
    {
    }

    /// <summary>
    /// 使用起始和结束日期初始化 <see cref="DatePeriod"/> 类的新实例。
    /// </summary>
    /// <param name="start">起始日期。</param>
    /// <param name="end">结束日期。</param>
    [JsonConstructor]
    public DatePeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
        Validate();
    }

    /// <summary>
    /// 获取区间的起始日期。
    /// </summary>
    public DateOnly Start { get; init; }

    /// <summary>
    /// 获取区间的结束日期。
    /// </summary>
    public DateOnly End { get; init; }

    /// <summary>
    /// 判断指定日期是否在当前区间内。
    /// </summary>
    /// <param name="value">要判断的日期。</param>
    /// <returns>如果在区间内返回 true，否则返回 false。</returns>
    public bool IsPresent(DateOnly value)
    {
        return value >= Start && value <= End;
    }

    /// <summary>
    /// 获取一个值，指示区间是否跨越了天。
    /// </summary>
    public bool IsCrossDay => Start != End;

    /// <summary>
    /// 判断当前区间是否与指定区间重叠。
    /// </summary>
    /// <param name="period">要比较的日期区间。</param>
    /// <returns>如果两个区间重叠则返回 true，否则返回 false。</returns>
    public bool IsOverlap(DatePeriod period)
    {
        return !(period.Start > End || period.End < Start);
    }

    /// <summary>
    /// 校验区间是否合法。
    /// </summary>
    /// <exception cref="ArgumentException">当起始日期大于结束日期时抛出。</exception>
    private void Validate()
    {
        if (Start > End)
        {
            throw new ArgumentException("开始日期不能大于结束日期.", nameof(Start));
        }
    }
}
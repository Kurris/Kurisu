using System;
using Kurisu.AspNetCore.CustomClass.Periods.Abstractions;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 表示一个日期时间区间。
/// </summary>
public class DateTimePeriod : IPeriod<DateTime>, IPeriodComparable<DateTime, DateTimePeriod>
{
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

    /// <summary>
    /// 天
    /// </summary>
    public int Days => (End - Start).Days;

    /// <summary>
    /// 小时
    /// </summary>
    public int Hours => (End - Start).Hours;

    /// <summary>
    /// 分钟
    /// </summary>
    public int Minutes => (End - Start).Minutes;

    /// <summary>
    /// 秒
    /// </summary>
    public int Seconds => (End - Start).Seconds;

    /// <summary>
    /// 毫秒
    /// </summary>
    public int Milliseconds => (End - Start).Milliseconds;

    /// <summary>
    /// 总天
    /// </summary>
    public double TotalDays => (End - Start).TotalDays;

    /// <summary>
    /// 总小时
    /// </summary>
    public double TotalHours => (End - Start).TotalHours;

    /// <summary>
    /// 总分钟
    /// </summary>
    public double TotalMinutes => (End - Start).TotalMinutes;

    /// <summary>
    /// 总秒
    /// </summary>
    public double TotalSeconds => (End - Start).TotalSeconds;

    /// <summary>
    /// 总毫秒
    /// </summary>
    public double TotalMilliseconds => (End - Start).TotalMilliseconds;


    /// <summary>
    /// tick
    /// </summary>
    public long Ticks => (End - Start).Ticks;
}
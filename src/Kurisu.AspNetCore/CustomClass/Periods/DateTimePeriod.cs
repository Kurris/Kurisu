using System;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 日期时分段
/// </summary>
public struct DateTimePeriod : IDateTimeComparable<DateTime, DateTimePeriod>
{
    public DateTimePeriod()
    {
    }

    public DateTimePeriod(string start, string end) : this(DateTime.Parse(start), DateTime.Parse(end))
    {
    }

    public DateTimePeriod(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? End { get; set; }

    /// <inheritdoc />
    public bool IsPresent(DateTime value)
    {
        return value >= Start && value <= End;
    }

    /// <inheritdoc />
    public bool IsCrossDay => Start!.Value.Date != End!.Value.Date;

    /// <inheritdoc />
    public bool IsOverlap(DateTimePeriod period)
    {
        return !(period.Start > End || period.End < Start);
    }

    /// <inheritdoc />
    public bool HasValue => Start.HasValue && End.HasValue;

    /// <summary>
    /// 天
    /// </summary>
    public int Days => this.HasValue ? (End!.Value - Start!.Value).Days : 0;
    
    /// <summary>
    /// 小时
    /// </summary>
    public int Hours => this.HasValue ? (End!.Value - Start!.Value).Hours : 0;
    
    /// <summary>
    /// 分钟
    /// </summary>
    public int Minutes => this.HasValue ? (End!.Value - Start!.Value).Minutes : 0;
    
    /// <summary>
    /// 秒
    /// </summary>
    public int Seconds => this.HasValue ? (End!.Value - Start!.Value).Seconds : 0;
    
    /// <summary>
    /// 毫秒
    /// </summary>
    public int Milliseconds => this.HasValue ? (End!.Value - Start!.Value).Milliseconds : 0;
    
    /// <summary>
    /// 总天
    /// </summary>
    public double TotalDays => this.HasValue ? (End!.Value - Start!.Value).TotalDays : 0;
    
    /// <summary>
    /// 总小时
    /// </summary>
    public double TotalHours => this.HasValue ? (End!.Value - Start!.Value).TotalHours : 0;
    
    /// <summary>
    /// 总分钟
    /// </summary>
    public double TotalMinutes => this.HasValue ? (End!.Value - Start!.Value).TotalMinutes : 0;
    
    /// <summary>
    /// 总秒
    /// </summary>
    public double TotalSeconds => this.HasValue ? (End!.Value - Start!.Value).TotalSeconds : 0;
    
    /// <summary>
    /// 总毫秒
    /// </summary>
    public double TotalMilliseconds => this.HasValue ? (End!.Value - Start!.Value).TotalMilliseconds : 0;
    
    
    /// <summary>
    /// tick
    /// </summary>
    public long Ticks => this.HasValue ? (End!.Value - Start!.Value).Ticks : 0;
}
using System;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 日期段
/// </summary>
public struct DatePeriod : IDateTimeComparable<DateOnly, DatePeriod>
{
    public DatePeriod()
    {
    }

    public DatePeriod(string start, string end) : this(DateOnly.Parse(start), DateOnly.Parse(end))
    {
    }

    public DatePeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    /// <inheritdoc />
    public DateOnly? Start { get; set; }

    /// <inheritdoc />
    public DateOnly? End { get; set; }

    /// <inheritdoc />
    public bool IsPresent(DateOnly value)
    {
        return value >= Start && value <= End;
    }

    /// <inheritdoc />
    public bool HasValue => Start.HasValue && End.HasValue;

    /// <inheritdoc />
    public bool IsCrossDay => Start != End;

    /// <inheritdoc />
    public bool IsOverlap(DatePeriod period)
    {
        return !(period.Start > End || period.End < Start);
    }
}
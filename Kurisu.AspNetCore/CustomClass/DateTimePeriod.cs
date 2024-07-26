using System;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 日期时分段
/// </summary>
public class DateTimePeriod
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? End { get; set; }
}

/// <summary>
/// DateTimePeriodExtensions
/// </summary>
public static class DateTimePeriodExtensions
{
    /// <summary>
    /// 存在值
    /// </summary>
    /// <param name="period"></param>
    /// <returns></returns>
    public static bool HasValue(this DateTimePeriod period)
    {
        return period is { Start: not null, End: not null };
    }
}
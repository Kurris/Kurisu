using System;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 日期端
/// </summary>
public class DatePeriod
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateOnly? Start { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateOnly? End { get; set; }
}

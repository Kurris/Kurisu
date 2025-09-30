using System;

namespace Kurisu.AspNetCore.CustomClass.Periods.Abstractions;

/// <summary>
/// 定义一个用于日期时间区间比较的接口。
/// </summary>
/// <typeparam name="TStruct">
/// 区间的基础值类型，必须为结构体并实现 <see cref="IComparable{T}"/>。
/// </typeparam>
/// <typeparam name="TPeriod">
/// 区间类型，必须实现 <see cref="IPeriod{TStruct}"/>。
/// </typeparam>
public interface IPeriodComparable<in TStruct, in TPeriod>
    where TStruct : struct, IComparable<TStruct>
    where TPeriod : IPeriod<TStruct>
{
    /// <summary>
    /// 判断指定值是否在当前区间内。
    /// </summary>
    /// <param name="value">要判断的值。</param>
    /// <returns>如果在区间内返回 true，否则返回 false。</returns>
    bool IsPresent(TStruct value);

    /// <summary>
    /// 当前区间是否跨天。
    /// </summary>
    public bool IsCrossDay { get; }

    /// <summary>
    /// 判断当前区间与指定区间是否有重叠。
    /// </summary>
    /// <param name="period">要比较的区间。</param>
    /// <returns>如果有重叠返回 true，否则返回 false。</returns>
    bool IsOverlap(TPeriod period);
}
using System;

namespace Kurisu.AspNetCore.CustomClass.Periods;

/// <summary>
/// 日期时间比较
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDateTimeComparable<T> where T : struct, IComparable<T>
{
    /// <summary>
    /// 开始
    /// </summary>
    public T? Start { get; set; }

    /// <summary>
    /// 结束
    /// </summary>
    public T? End { get; set; }

    /// <summary>
    /// 是否在区间内
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    bool IsPresent(T value);

    /// <summary>
    /// 不为空
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// 是否跨天
    /// </summary>
    public bool IsCrossDay { get; }
}

/// <summary>
/// 日期时间比较
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TPeriod"></typeparam>
//where T : struct, IComparable<T> where TPeriod : struct
public interface IDateTimeComparable<T, in TPeriod> : IDateTimeComparable<T> where T : struct, IComparable<T>
{
    /// <summary>
    /// 是否重合
    /// </summary>
    /// <param name="period"></param>
    /// <returns></returns>
    bool IsOverlap(TPeriod period);
}
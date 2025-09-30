using System;

namespace Kurisu.AspNetCore.CustomClass.Periods.Abstractions;

/// <summary>
/// 表示一个通用的时间段接口。
/// </summary>
/// <typeparam name="TStruct">
/// 时间段的类型参数，必须为值类型并实现 <see cref="IComparable{T}"/> 接口。
/// </typeparam>
public interface IPeriod<TStruct> where TStruct : struct, IComparable<TStruct>
{
    /// <summary>
    /// 获取时间段的起始值。
    /// </summary>
    public TStruct Start { get; init; }

    /// <summary>
    /// 获取时间段的结束值。
    /// </summary>
    public TStruct End { get; init; }
}
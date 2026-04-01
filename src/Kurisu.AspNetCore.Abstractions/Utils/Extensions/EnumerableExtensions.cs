using System;
using System.Collections.Generic;
using System.Linq;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 集合扩展类
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// 集合string join
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string Join<T>(this IEnumerable<T> enumerable, string separator)
    {
        return string.Join(separator, enumerable);
    }

    /// <summary>
    /// 集合string join
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string Join<T>(this IEnumerable<T> enumerable, char separator)
    {
        return string.Join(separator, enumerable);
    }

    /// <summary>
    /// 过滤条件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="condition"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> enumerable, bool condition, Func<T, bool> predicate)
    {
        return condition
            ? enumerable.Where(predicate)
            : enumerable;
    }

    /// <summary>
    /// 为空
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            return true;
        }

        return !enumerable.Any();
    }

    /// <summary>
    /// 存在
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static bool IsPresent<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.IsEmpty();
    }
}
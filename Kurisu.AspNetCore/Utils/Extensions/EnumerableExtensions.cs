using System;
using System.Collections.Generic;
using System.Linq;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 集合扩展类
/// </summary>
public static class EnumerableExtensions
{
    public static string Join<T>(this IEnumerable<T> enumerable, string separator)
    {
        return string.Join(separator, enumerable);
    }

    public static string Join<T>(this IEnumerable<T> enumerable, char separator)
    {
        return string.Join(separator, enumerable);
    }

    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> enumerable, bool condition, Func<T, bool> predicate)
    {
        return condition
          ? enumerable.Where(predicate)
          : enumerable;
    }
}
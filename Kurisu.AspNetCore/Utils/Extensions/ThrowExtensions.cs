using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.CustomClass;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// throw helper
/// </summary>
public static class ThrowExtensions
{
    /// <summary>
    /// null异常抛出
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfNull<T>(this T obj, string errorMessage) where T : class
    {
        if (obj == null)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    /// <summary>
    /// true异常抛出 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfTrue(this bool condition, string errorMessage)
    {
        if (condition)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    /// <summary>
    /// false异常抛出
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfFalse(this bool condition, string errorMessage)
    {
        if (!condition)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    /// <summary>
    /// notfound异常抛出
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfEmpty<T>(this IEnumerable<T> values, string errorMessage)
    {
        if (values?.Any() != true)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    /// <summary>
    /// exists异常抛出
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfExists<T>(this IEnumerable<T> values, string errorMessage)
    {
        if (values != null && values.Any())
        {
            throw new UserFriendlyException(errorMessage);
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;

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
    [MemberNotNull]
    public static void ThrowIfNull<T>([NotNull] this T obj, string errorMessage) where T : class
    {
        if (obj == null)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    /// <summary>
    /// not null异常抛出
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="errorMessage"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfNotNull<T>(this T obj, string errorMessage) where T : class
    {
        if (obj != null)
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
    /// NotFound异常抛出
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public static void ThrowIfEmpty<T>([NotNull] this IEnumerable<T> values, string errorMessage)
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
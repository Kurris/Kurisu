using System.Collections.Generic;
using System.Linq;
using Kurisu.AspNetCore.UnifyResultAndValidation;

namespace Kurisu.AspNetCore.Utils.Extensions;

public static class ThrowExtensions
{
    public static void ThrowIfNull<T>(this T obj, string errorMessage) where T : class
    {
        if (obj == null)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    public static void ThrowIfTrue(this bool condition, string errorMessage)
    {
        if (condition)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }


    public static void ThrowIfFalse(this bool condition, string errorMessage)
    {
        if (!condition)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }

    public static void ThrowIfEmpty<T>(this IEnumerable<T> values, string errorMessage)
    {
        if (values?.Any() != true)
        {
            throw new UserFriendlyException(errorMessage);
        }
    }


    public static void ThrowIfExists<T>(this IEnumerable<T> values, string errorMessage)
    {
        if (values != null && values.Any())
        {
            throw new UserFriendlyException(errorMessage);
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.AspNetCore.Abstractions.Result;

/// <summary>
/// 表示一个用户友好异常，不会产生日志和堆栈信息。
/// </summary>
/// <remarks>
/// 用于向前端返回友好的错误信息，避免暴露系统内部细节。
/// </remarks>
[SkipScan]
public class UserFriendlyException : Exception
{
    public ExceptionContext ExceptionContext { get; set; }

    /// <summary>
    /// 使用指定的错误信息和异常初始化 <see cref="UserFriendlyException"/> 类的新实例。
    /// </summary>
    public UserFriendlyException() : base("操作失败，请稍后重试")
    {
    }
    
    /// <summary>
    /// 使用指定的错误信息初始化 <see cref="UserFriendlyException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误信息。</param>
    public UserFriendlyException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定的异常初始化 <see cref="UserFriendlyException"/> 类的新实例。
    /// </summary>
    /// <param name="exception">原始异常。</param>
    public UserFriendlyException(Exception exception) : base(exception.Message)
    {
    }

    /// <summary>
    /// 使用指定的错误信息和异常初始化 <see cref="UserFriendlyException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误信息。</param>
    /// <param name="exception">原始异常。</param>
    public UserFriendlyException(string message, Exception exception) : base(message, exception)
    {
    }
}



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
    [MemberNotNull(nameof(obj))]
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
    [MemberNotNull(nameof(values))]
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
using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;

/// <summary>
/// 表示一个用户友好异常，不会产生日志和堆栈信息。
/// </summary>
/// <remarks>
/// 用于向前端返回友好的错误信息，避免暴露系统内部细节。
/// </remarks>
[SkipScan]
public class UserFriendlyException : Exception
{
    internal ExceptionContext ExceptionContext { get; set; }

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
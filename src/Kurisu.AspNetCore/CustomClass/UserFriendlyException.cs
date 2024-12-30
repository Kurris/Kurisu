using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 用户友好异常
/// </summary>
/// <remarks>
/// 不产生日志和堆栈信息
/// </remarks>
[SkipScan]
public class UserFriendlyException : Exception
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="message">错误信息</param>
    public UserFriendlyException(string message) : base(message)
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="exception">异常</param>
    public UserFriendlyException(Exception exception) : base(exception.Message)
    {
    }


    /// <summary>
    ///c tor
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public UserFriendlyException(string message, Exception exception) : base(message, exception)
    {
    }
}
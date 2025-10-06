using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.AspNetCore;

/// <summary>
/// 日志
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LogAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="title"></param>
    public LogAttribute(string title)
    {
        Title = title;
    }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 禁止响应输出
    /// </summary>
    public bool DisableResponseLogout { get; set; }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();
    }
}
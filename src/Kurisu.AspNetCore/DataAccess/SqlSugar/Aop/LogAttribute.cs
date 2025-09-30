using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;


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
    public string Title { get; set; }
    
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        PreSetting(context.HttpContext);
        await next();
    }

    private void PreSetting(HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetService<ISqlSugarOptionsService>();

        options.Title = Title;
        options.BatchNo = Guid.NewGuid();
        options.RoutePath = httpContext.Request.Path.Value;
        options.RaiseTime = DateTime.Now;
    }
}

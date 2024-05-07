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
    public LogAttribute(string title)
    {
        Title = title;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 是否处理差异
    /// </summary>
    public bool Diff { get; set; }


    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        PreSetting(context.HttpContext);
        await next();
    }

    private void PreSetting(HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetService<ISqlSugarOptionsService>();

        options.Title = Title;
        options.Diff = Diff;
        options.BatchNo = Guid.NewGuid();
        options.RoutePath = httpContext.Request.Path.Value;
        options.RaiseTime = DateTime.Now;
    }
}

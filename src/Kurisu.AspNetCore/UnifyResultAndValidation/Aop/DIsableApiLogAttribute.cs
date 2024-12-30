using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Aop;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DisableApiLogAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var setting = context.HttpContext.RequestServices.GetService<ApiRequestSettingService>();
        setting.EnableApiRequestLog = false;
        await next();
    }
}
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Filters;

/// <summary>
/// 实体验证和包装返回值过滤器
/// </summary>
[SkipScan]
// ReSharper disable once ClassNeverInstantiated.Global
public class ValidateAndPackResultFilter : IAsyncActionFilter, IAsyncResultFilter, IOrderedFilter
{
    /// <summary>
    /// 优先执行
    /// </summary>
    public int Order => -999;

    /// <summary>
    /// 请求参数处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var apiLogSetting = context.HttpContext.RequestServices.GetRequiredService<ApiLogSetting>();
        if (apiLogSetting.EnableApiRequestLog)
        {
            apiLogSetting.ConnectionId = context.HttpContext.Connection.Id;
            apiLogSetting.Path = context.HttpContext.Request.Path;
            apiLogSetting.HttpMethod = context.HttpContext.Request.Method;
            apiLogSetting.Parameters = context.ActionArguments;
            apiLogSetting.UserId = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>().GetUserId();
        }

        var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var logAttribute = controllerActionDescriptor.MethodInfo.GetCustomAttribute<LogAttribute>() ?? new LogAttribute(string.Empty);
        apiLogSetting.Title = logAttribute.Title;
        apiLogSetting.DisableResponseLogout = logAttribute.DisableResponseLogout;

        if (apiLogSetting.Title.IsPresent())
        {
            using (LogContext.PushProperty("Prefix", $"[{apiLogSetting.Title}]"))
            {
                await next();
            }
        }
        else
        {
            await next();
        }
    }

    /// <summary>
    /// 请求结果处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var frameworkExceptionHandlers = context.HttpContext.RequestServices.GetRequiredService<IFrameworkExceptionHandlers>();

        //实体验证错误400
        if (!context.ModelState.IsValid)
        {
            await frameworkExceptionHandlers.HandleAsync(new ModelStateNotValidException(context));
        }
        else
        {
            ResultHandle(context);
        }

        //日志
        var apiLogSetting = context.HttpContext.RequestServices.GetRequiredService<ApiLogSetting>();
        if (apiLogSetting.EnableApiRequestLog)
        {
            if (apiLogSetting.Title.IsPresent())
            {
                using (LogContext.PushProperty("Prefix", $"[{apiLogSetting.Title}]"))
                {
                    if (!apiLogSetting.DisableResponseLogout)
                    {
                        apiLogSetting.Response = (context.Result as ObjectResult)?.Value;
                    }

                    apiLogSetting.Log();
                    await next();
                }
            }
            else
            {
                await next();
            }
        }
        else
        {
            await next();
        }
    }

    private static void ResultHandle(ResultExecutingContext context)
    {
        var apiResult = context.HttpContext.RequestServices.GetRequiredService<IApiResult>();
        //200
        switch (context.Result)
        {
            //实体对象，如果是FileResultContent则不会进入
            case ObjectResult objectResult:
            {
                var result = objectResult.Value;
                var type = result?.GetType() ?? typeof(object);

                var isApiResult = type.IsAssignableTo(typeof(IApiResult));
                if (!isApiResult)
                {
                    context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(result));
                }

                break;
            }
            //空task
            case EmptyResult:
                context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(null as object));
                break;
        }
    }
}
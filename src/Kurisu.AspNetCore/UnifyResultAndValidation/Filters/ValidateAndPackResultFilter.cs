using System.Reflection;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;
using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        var setting = context.HttpContext.RequestServices.GetService<ApiRequestSettingService>();
        if (setting?.EnableApiRequestLog == true)
        {
            setting.ConnectionId = context.HttpContext.Connection.Id;
            setting.Path = context.HttpContext.Request.Path;
            setting.Method = context.HttpContext.Request.Method;
            setting.Parameters = JsonConvert.SerializeObject(context.ActionArguments);
            setting.UserId = context.HttpContext.RequestServices.GetService<ICurrentUser>()?.GetUserId();
        }

        var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var logAttribute = controllerActionDescriptor.MethodInfo.GetCustomAttribute<LogAttribute>();
        var title = logAttribute?.Title;
        if (title.IsPresent())
        {
            using (LogContext.PushProperty("Prefix", $"[{title}]"))
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
        var setting = context.HttpContext.RequestServices.GetService<ApiRequestSettingService>();
        if (setting?.EnableApiRequestLog == true)
        {
            setting.Response = (context.Result as ObjectResult)?.Value?.ToJson();
            setting.Log();
        }

        await next();
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
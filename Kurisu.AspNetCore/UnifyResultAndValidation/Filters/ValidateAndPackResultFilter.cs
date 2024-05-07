using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation.Options;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Filters;

/// <summary>
/// 实体验证和包装返回值过滤器
/// </summary>
[SkipScan]
// ReSharper disable once ClassNeverInstantiated.Global
public class ValidateAndPackResultFilter : IAsyncActionFilter, IAsyncResultFilter
{
    /// <summary>
    /// 请求参数处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //默认开启日志
        var filterOptions = context.HttpContext.RequestServices.GetService<IOptions<FilterOptions>>().Value;
        if (filterOptions == null || filterOptions.EnableApiRequestLog)
        {
            //var desc = context.ActionDescriptor as ControllerActionDescriptor;
            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            var parameters = context.ActionArguments.ToJson(JsonExtensions.DefaultSetting);
            var logger = context.HttpContext.RequestServices.GetService<ILogger<ValidateAndPackResultFilter>>();
            logger.LogInformation("[ApiLog] Request: {method} {path} \r\nParams:{params}", method, path, parameters);
        }

        //请求前
        await next();
        //请求后
    }

    /// <summary>
    /// 请求结果处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var apiResult = context.HttpContext.RequestServices.GetService<IApiResult>()!;

        //实体验证错误
        if (!context.ModelState.IsValid)
        {
            var errorResults = context.ModelState.Keys.SelectMany(key =>
                    context.ModelState[key]!.Errors.Where(_ => !string.IsNullOrEmpty(key))
                        .Select(x => new
                        {
                            Field = key,
                            Message = x.ErrorMessage
                        }))
                .ToList();

            string msg;
            if (!errorResults.Any())
            {
                msg = ":参数为空";
            }
            else
            {
                var es = errorResults.Select(x => x.Message).Distinct();
                msg = ":" + es.Join(",");
            }

            //包装验证错误信息
            context.Result = new ObjectResult(apiResult.GetDefaultValidateApiResult(msg));
        }
        else
        {
            switch (context.Result)
            {
                //实体对象，如果是FileResultContent/IActionResult则不会进入
                case ObjectResult objectResult:
                {
                    var result = objectResult.Value;
                    var type = result?.GetType() ?? typeof(object);
                    //返回值已经包装
                    if (type.IsGenericType && type.IsAssignableTo(typeof(IApiResult)))
                        context.Result = new ObjectResult(result);
                    else
                        context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(result));
                    break;
                }
                //空task
                case EmptyResult:
                    context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult((object)null));
                    break;
            }
        }

        await next();
    }
}
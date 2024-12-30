using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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
        var setting = context.HttpContext.RequestServices.GetService<ApiRequestSettingService>();
        if (setting.EnableApiRequestLog)
        {
            setting.ConnectionId = context.HttpContext.Connection.Id;
            setting.Path = context.HttpContext.Request.Path;
            setting.Method = context.HttpContext.Request.Method;
            setting.Parameters = JsonConvert.SerializeObject(context.ActionArguments);
            setting.UserId = context.HttpContext.RequestServices.GetService<ICurrentUser>()?.GetIntSubjectId();
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

            var msg = string.Empty;
            if (!errorResults.Any())
                msg += "None Body Request";
            else
            {
                if (errorResults.Count > 1)
                {
                    var es = errorResults.Select(x => x.Message);
                    msg += "\r\n" + es.Join("\r\n");
                }
                else
                {
                    var es = errorResults.Select(x => x.Message).Distinct();
                    msg += es.Join(",");
                }
            }

            //包装验证错误信息
            context.Result = new ObjectResult(apiResult.GetDefaultValidateApiResult(msg));
        }
        else
        {
            switch (context.Result)
            {
                //实体对象，如果是FileResultContent则不会进入
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

        //日志
        var setting = context.HttpContext.RequestServices.GetService<ApiRequestSettingService>();
        if (setting.EnableApiRequestLog)
        {
            setting.Response = (context.Result as ObjectResult)?.Value?.ToJson();
            setting.Log();
        }

        await next();
    }
}
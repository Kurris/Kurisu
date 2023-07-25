using System.Linq;
using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable ClassNeverInstantiated.Global

namespace Kurisu.UnifyResultAndValidation.Filters;

/// <summary>
/// 实体验证和包装返回值过滤器
/// </summary>
[SkipScan]
public class ValidateAndPackResultFilter : IAsyncActionFilter, IAsyncResultFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //请求前
        await next();
        //请求后
    }

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
                    }));

            //包装验证错误信息
            context.Result = new ObjectResult(apiResult.GetDefaultValidateApiResult(errorResults));
        }
        else
        {
            switch (context.Result)
            {
                //实体对象，如果是FileResultContent/IActionResult则不会进入
                case ObjectResult objectResult:
                {
                    var result = objectResult.Value ?? string.Empty;
                    var type = result.GetType();

                    //返回值已经包装
                    if (type.IsGenericType && type.IsAssignableTo(typeof(IApiResult)))
                        context.Result = new ObjectResult(result);
                    else
                        context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(result));
                    break;
                }
                //空task
                case EmptyResult:
                    context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult((object) null));
                    break;
            }
        }

        await next();
    }
}
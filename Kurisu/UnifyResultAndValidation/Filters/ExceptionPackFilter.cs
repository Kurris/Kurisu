using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable ClassNeverInstantiated.Global

namespace Kurisu.UnifyResultAndValidation.Filters;

/// <summary>
/// 异常过滤器
/// </summary>
[SkipScan]
public class ExceptionPackFilter : IAsyncExceptionFilter
{
    /// <summary>
    ///将请求管道中出现的特定异常以httpCode200的方式返回到请求方表示正确请求但处理中出现问题
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not UserFriendlyException)
            return Task.CompletedTask;

        var apiResult = context.HttpContext.RequestServices.GetService<IApiResult>()!;
        context.Result = new ObjectResult(apiResult.GetDefaultErrorApiResult(context.Exception.Message));
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
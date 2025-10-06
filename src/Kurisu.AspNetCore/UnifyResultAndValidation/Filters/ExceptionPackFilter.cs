using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.CustomClass;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Filters;

/// <summary>
/// 异常过滤器
/// </summary>
[SkipScan]
// ReSharper disable once ClassNeverInstantiated.Global
public class ExceptionPackFilter : IAsyncExceptionFilter
{
    /// <summary>
    ///将请求管道中出现的特定异常以httpCode200的方式返回到请求方表示正确请求但处理中出现问题
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is UserFriendlyException ex)
        {
            ex.ExceptionContext = context;
            var exceptionHandlers = context.HttpContext.RequestServices.GetRequiredService<IFrameworkExceptionHandlers>();
            if (await exceptionHandlers.HandleAsync(ex))
            {
                context.ExceptionHandled = true;
            } 
        }
    }
}
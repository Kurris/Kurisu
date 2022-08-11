using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation.Filters
{
    /// <summary>
    /// 异常过滤器
    /// </summary>
    [SkipScan]
    public class ExceptionPackFilter : IAsyncExceptionFilter
    {
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is UserFriendlyException)
            {
                var apiResult = context.HttpContext.RequestServices.GetService<IApiResult>();
                context.Result = new ObjectResult(apiResult.GetDefaultErrorApiResult(context.Exception.Message));
                context.ExceptionHandled = true;
            }

            return Task.CompletedTask;
        }
    }
}
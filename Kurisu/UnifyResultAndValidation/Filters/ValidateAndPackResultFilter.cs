using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation.Filters
{
    /// <summary>
    /// 实体验证和包装返回值过滤器
    /// </summary>
    public class ValidateAndPackResultFilter : IAsyncActionFilter, IAsyncResultFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var apiResult = serviceProvider.GetService<IApiResult>();
            if (apiResult == null)
            {
                throw new ArgumentNullException(nameof(IApiResult));
            }

            //实体验证错误
            if (!context.ModelState.IsValid)
            {
                var errorResults = context.ModelState.Keys.SelectMany(key =>
                    context.ModelState[key].Errors.Where(_ => !string.IsNullOrEmpty(key))
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
                //实体对象，如果是FileResultContent/IActionResult则不会进入
                if (context.Result is ObjectResult objectResult)
                {
                    var result = objectResult.Value ?? string.Empty;
                    var type = result.GetType();

                    //返回值已经包装
                    if (type.IsGenericType && type.IsAssignableTo(typeof(IApiResult)))
                        context.Result = new ObjectResult(result);
                    else
                        context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(result));
                }
                //空 task
                else if (context.Result is EmptyResult)
                {
                    context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult((object) null));
                }
            }

            await next();
        }
    }
}
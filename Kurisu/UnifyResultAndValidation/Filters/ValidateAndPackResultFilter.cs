using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation.Filters
{
    public class ValidateAndPackResultFilter : ActionFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var provider = context.HttpContext.RequestServices;
            var apiResult = provider.GetService<IApiResult>();
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
                    if (type.IsGenericType && typeof(IApiResult).IsAssignableFrom(type))
                        context.Result = new ObjectResult(result);
                    else
                    {
                        //获取自定义包装处理
                        context.Result = new ObjectResult(apiResult.GetDefaultSuccessApiResult(result));
                    }
                }
            }

            await base.OnResultExecutionAsync(context, next);
        }
    }
}
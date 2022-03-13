using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation.Filters
{
    public class ValidateFilter : IAsyncResultFilter
    {
        public ValidateFilter()
        {

        }


        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errorResults = context.ModelState
                    .Keys
                    .SelectMany(key =>
                        context.ModelState[key].Errors
                        .Where(x => !string.IsNullOrEmpty(key))
                        .Select(x => new
                        {
                            Field = key,
                            Message = x.ErrorMessage
                        }));

                context.Result = new ObjectResult(
                    new ApiResult<IEnumerable<object>>
                    (
                        "参数不合法",
                        errorResults,
                        Status.ValidateError
                    )
                );
            }
            else
            {
                if (context.Result is ObjectResult objectResult)
                {
                    var result = objectResult.Value ?? string.Empty;
                    var type = result.GetType();

                    if (type.IsGenericType && typeof(IApiResult).IsAssignableFrom(type))
                        context.Result = new ObjectResult(result);
                    else
                    {
                        var injectApiResult = context.HttpContext.RequestServices.GetService<IApiResult>();
                        context.Result = new ObjectResult(injectApiResult.GetDefaultSuccessApiResult(result));
                    }
                }
            }

            await next();
        }
    }
}
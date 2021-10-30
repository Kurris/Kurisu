using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.UnifyResultAndValidation.Filters
{
    public class ValidateFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errorResults = context.ModelState.Keys.SelectMany(key =>
                    context.ModelState[key].Errors.Where(x => !string.IsNullOrEmpty(key)).Select(x => new
                    {
                        Field = key,
                        Message = x.ErrorMessage
                    }));

                context.Result = new ObjectResult(
                    new ApiResult<IEnumerable<object>>
                    (
                        "参数不合法",
                        errorResults,
                        Status.ValidateEntityError
                    )
                );
            }
            else
            {
                var successResult = (context.Result as ObjectResult)?.Value;

                if (successResult != null)
                {
                    context.Result = successResult.GetType().Name.StartsWith("ApiResult")
                        ? new ObjectResult(successResult)
                        : new ObjectResult(new ApiResult<object>("请求成功", successResult, Status.Success));
                }
            }

            await next();
        }
    }
}
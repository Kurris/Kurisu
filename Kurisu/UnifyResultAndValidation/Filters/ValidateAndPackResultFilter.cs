using System.Linq;
using System.Threading.Tasks;
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

            if (!context.ModelState.IsValid)
            {
                var errorResults = context.ModelState.Keys.SelectMany(key =>
                    context.ModelState[key].Errors.Where(x => !string.IsNullOrEmpty(key)).Select(x => new
                    {
                        Field = key,
                        Message = x.ErrorMessage
                    }));

                var apiResult = provider.GetService<IApiResult>();

                context.Result = new ObjectResult(apiResult.GetDefaultValidateResult(errorResults));
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
                        var apiResult = provider.GetService<IApiResult>();
                        context.Result = new ObjectResult(apiResult.GetDefaultSuccessResult(result));
                    }
                }
            }

            await base.OnResultExecutionAsync(context, next);
        }
    }
}
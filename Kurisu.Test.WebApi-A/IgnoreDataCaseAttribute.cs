using System.Text;
using Kurisu.Core.Result;
using Kurisu.Core.Result.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Kurisu.Test.WebApi_A;

[AttributeUsage(AttributeTargets.Method)]
public class IgnoreDataCaseAttribute : Attribute, IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult result)
        {
            var type = result.Value?.GetType();
            if (type.IsGenericType && type.IsAssignableTo(typeof(IApiResult)))
            {
                if (result.Value is DefaultApiResult<object> apiResult && !context.HttpContext.Response.HasStarted)
                {
                    var body = new
                    {
                        code = 200,
                        msg = "操作成功",
                        data = apiResult.Data
                    };

                    var json = JsonConvert.SerializeObject(body);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    context.HttpContext.Response.StatusCode = 200;
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.ContentLength = bytes.Length;
                    await context.HttpContext.Response.BodyWriter.WriteAsync(bytes);
                    await context.HttpContext.Response.StartAsync();
                }
            }
        }

        await next();
    }
}

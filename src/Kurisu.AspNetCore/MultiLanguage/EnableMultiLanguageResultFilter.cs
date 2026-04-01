using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Result;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;

namespace Kurisu.AspNetCore.MultiLanguage;

/// <summary>
/// 启用多语言
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EnableMultiLanguageAttribute : Attribute, IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.HttpContext.Request.Headers.TryGetValue(App.StartupOptions.LanguageHeaderName, out var values))
        {
            var headerValue = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                // parse header: handle values like "en-US,en;q=0.9" or "en;q=0.8"
                var language = headerValue.Split(',').First().Split(';').First().Trim();
                if (!string.IsNullOrEmpty(language))
                {
                    if (context.Result is ObjectResult objectResult)
                    {
                        var originalResult = objectResult.Value;
                        if (originalResult != null)
                        {
                            if (originalResult is IApiResult apiResult)
                            {
                                var data = apiResult.GetData();
                                var msg = apiResult.GetMsg();

                                var newData = LanguageHandler.HandleResult(language, data);

                                var newApiResult = apiResult.Reset(newData, msg);

                                context.Result = new ObjectResult(JToken.Parse(newApiResult.ToJson(JsonExtensions.DefaultSetting)));
                            }
                            else
                            {
                                context.Result = new ObjectResult(JToken.Parse(LanguageHandler.HandleResult(language, originalResult).ToJson(JsonExtensions.DefaultSetting)));
                            }
                        }
                    }
                }
            }
        }

        await next();
    }
}
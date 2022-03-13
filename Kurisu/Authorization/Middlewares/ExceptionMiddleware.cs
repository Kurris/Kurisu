using System;
using System.Text;
using System.Threading.Tasks;
using Kurisu.UnifyResultAndValidation;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace Kurisu.Authorization.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                if (!context.User.Identity.IsAuthenticated)
                {
                    if (context.Response.HasStarted) return;

                    context.Response.StatusCode = 200;
                    var apiResult = context.RequestServices.GetService<IApiResult>();
                    var resultJson = JsonConvert.SerializeObject(apiResult.GetDefaultForbiddenApiResult());
                    var content = Encoding.UTF8.GetBytes(resultJson);

                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength = content.Length;
                    await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(content));
                }
            }
            catch (Exception ex)
            {
                var msg = ex.GetBaseException().Message;
                _logger.LogError(msg);

                context.Response.StatusCode = 200;
                var apiResult = context.RequestServices.GetService<IApiResult>();
                var resultJson = JsonConvert.SerializeObject(apiResult.GetDefaultErrorApiResult(msg));
                var content = Encoding.UTF8.GetBytes(resultJson);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = content.Length;
                await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(content));
            }
        }
    }
}
using System;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Startup.Abstractions;
using Kurisu.UnifyResultAndValidation.Abstractions;
using Kurisu.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.AppPacks
{
    /// <summary>
    /// 默认全局异常中间件启动pack
    /// </summary>
    public class DefaultGlobalExceptionPack : BaseAppPack
    {
        public override int Order => 0;
        public override bool IsBeforeUseRouting => true;

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }

    /// <summary>
    /// 全局异常中间件
    /// </summary>
    public class GlobalExceptionMiddleware : BaseMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next) : base(next)
        {
            _next = next;
        }

        public override async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                context.Response.StatusCode = ex.Source.Contains("IdentityModel.AspNetCore") ? 401 : 500;

                var apiResult = context.RequestServices.GetService<IApiResult>();

                byte[] content = Encoding.UTF8.GetBytes(apiResult.GetDefaultErrorApiResult(errorMessage).ToJson());

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = content.Length;
                await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(content));

                //异常堆栈显示console
                throw;
            }
        }
    }
}
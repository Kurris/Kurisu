using System;
using System.Text;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Startup.Abstractions;
using Kurisu.AspNetCore.Utils.Extensions;
using Kurisu.Core.Result.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Startup.AppPacks;

/// <summary>
/// 默认全局异常中间件启动pack
/// </summary>
public class DefaultGlobalExceptionPack : BaseAppPack
{
    /// <summary>
    /// 优先级
    /// </summary>
    public override int Order => -1;

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
    public GlobalExceptionMiddleware(RequestDelegate next) : base(next)
    {
    }

    public override async Task Invoke(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetService<ILogger<GlobalExceptionMiddleware>>();
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ex.Source!.Contains("IdentityModel.AspNetCore") ? 401 : 500;
                var apiResult = context.RequestServices.GetService<IApiResult>();

                //驼峰
                var json = JsonConvert.SerializeObject(apiResult!.GetDefaultErrorApiResult(ex.Message), JsonExtensions.DefaultSetting);
                var bytes = Encoding.UTF8.GetBytes(json);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = bytes.Length;
                await context.Response.BodyWriter.WriteAsync(bytes);
            }

            logger.LogError(ex, "[ApiLog] Global Exception:{path} {message}", context.Request.Path, ex.Message);

            //异常堆栈显示在console
            throw;
        }
    }
}
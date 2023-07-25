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

namespace Kurisu.Startup.AppPacks;

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
            context.Response.StatusCode = ex.Source!.Contains("IdentityModel.AspNetCore") ? 401 : 500;
            var apiResult = context.RequestServices.GetService<IApiResult>();

            var bytes = Encoding.UTF8.GetBytes(apiResult!.GetDefaultErrorApiResult(ex.Message).ToJson());

            context.Response.ContentType = "application/json";
            context.Response.ContentLength = bytes.Length;
            await context.Response.BodyWriter.WriteAsync(bytes);

            //异常堆栈显示在console
            throw;
        }
    }
}
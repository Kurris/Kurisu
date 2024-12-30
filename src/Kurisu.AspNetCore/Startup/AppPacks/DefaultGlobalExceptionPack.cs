using System;
using System.Text;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Startup.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    /// <inheritdoc />
    public override bool IsBeforeUseRouting => true;

    /// <inheritdoc />
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
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="next"></param>
    public GlobalExceptionMiddleware(RequestDelegate next) : base(next)
    {
    }

    /// <summary>
    /// invoke
    /// </summary>
    /// <param name="context"></param>
    public override async Task Invoke(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ex.Source!.Contains("IdentityModel.AspNetCore") ? 401 : 500;
                var apiResult = context.RequestServices.GetService<IApiResult>();

                //驼峰
                var responseJson = JsonConvert.SerializeObject(apiResult!.GetDefaultErrorApiResult(ex.Message), JsonExtensions.DefaultSetting);
                var bytes = Encoding.UTF8.GetBytes(responseJson);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = bytes.Length;
                await context.Response.BodyWriter.WriteAsync(bytes);
                var setting = context.RequestServices.GetService<ApiRequestSettingService>();
                if (setting.EnableApiRequestLog)
                {
                    setting.IsGlobal = true;
                    setting.Response = responseJson;
                    setting.Log();
                }
            }

            //异常堆栈显示在console
            throw;
        }
    }
}
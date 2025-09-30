using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Startup.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
            var frameworkExceptionHandlers = context.RequestServices.GetRequiredService<IFrameworkExceptionHandlers>();
            await frameworkExceptionHandlers.HandleAsync(ex);
        }
    }
}
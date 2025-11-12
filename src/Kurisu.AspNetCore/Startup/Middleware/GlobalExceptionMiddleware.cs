using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.Middleware;

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
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Extensions.ContextAccessor.Default;

/// <summary>
///  状态生命周期中间件
/// </summary>
public class ContextAccessorMiddleware : BaseMiddleware
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="next"></param>
    public ContextAccessorMiddleware(RequestDelegate next) : base(next)
    {
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="context"></param>
    public override async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.Value.StartsWith("/api/"))
        {
            await Next(context);
        }
        else
        {
            using (context.RequestServices.InitLifecycle())
            {
                await Next(context);
            }
        }
    }
}
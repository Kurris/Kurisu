using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Abstractions.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.Middleware;

/// <summary>
///  状态生命周期中间件
/// </summary>
public class StateMiddleware : BaseMiddleware
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="next"></param>
    public StateMiddleware(RequestDelegate next) : base(next)
    {
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="context"></param>
    public override async Task Invoke(HttpContext context)
    {
        var lifecycles = context.RequestServices.GetServices<IStateLifecycle>().ToList();
        foreach (var lifecycle in lifecycles)
        {
            try
            {
                lifecycle.Initialize();
            }
            catch
            {
                // best-effort
            }
        }

        try
        {
            await Next(context);
        }
        finally
        {
            foreach (var lifecycle in lifecycles)
            {
                try
                {
                    lifecycle.Remove();
                }
                catch
                {
                    // swallow
                }
            }
        }
    }
}
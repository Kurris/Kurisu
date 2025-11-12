using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Startup.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Kurisu.AspNetCore.Startup.Module;

/// <summary>
/// 状态中间件
/// </summary>
public class DefaultStateModule : AppModule
{
    /// <summary>
    /// order
    /// </summary>
    public override int Order => -998;

    /// <summary>
    /// 在路由之前使用
    /// </summary>
    public override bool IsBeforeUseRouting => true;

    /// <summary>
    /// 管道执行
    /// </summary>
    /// <param name="app"></param>
    public override void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<StateMiddleware>();
    }
}
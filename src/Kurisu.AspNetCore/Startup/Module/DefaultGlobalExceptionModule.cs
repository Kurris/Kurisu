using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Startup.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Kurisu.AspNetCore.Startup.Module;

/// <summary>
/// 默认全局异常中间件启动pack
/// </summary>
public class DefaultGlobalExceptionModule : AppModule
{
    public override string Name => "全局异常处理模块";

    /// <summary>
    /// 优先级
    /// </summary>
    public override int Order => -999;

    /// <inheritdoc />
    public override bool IsBeforeUseRouting => true;

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
    }


}
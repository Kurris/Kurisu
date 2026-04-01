using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;

namespace Kurisu.Extensions.ContextAccessor.Default;

/// <summary>
/// 上下文模块
/// </summary>
public class DefaultOperateContextModule : AppModule
{
    public override string Name => "操作状态模块";

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
        app.UseMiddleware<ContextAccessorMiddleware>();
    }
}
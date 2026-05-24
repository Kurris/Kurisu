using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 根据权限跨租户
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EnableCrossTenantAttribute : AopAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var ctx = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (ctx.EnableCrossTenant())
        {
            await next(context);
        }
    }
}
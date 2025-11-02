using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 忽略租户
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IgnoreTenantAttribute : AopAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        await dbContext.IgnoreTenantAsync(async () => { await next(context); });
    }
}
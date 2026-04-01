using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 忽略分片特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreShardingAttribute : AopAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (dbContext.IgnoreSharding())
        {
            await next(context);
        }
    }
}

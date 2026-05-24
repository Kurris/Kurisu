using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 使用数据权限
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EnableDataPermissionAttribute : AopAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (dbContext.EnableDataPermission())
        {
            await next(context);
        }
    }
}
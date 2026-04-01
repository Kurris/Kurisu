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
    private readonly Type[] _ignoreTypes;

    /// <summary>
    /// 数据权限
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public EnableDataPermissionAttribute(params Type[] ignoreTypes)
    {
        _ignoreTypes = ignoreTypes;
    }

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (dbContext.EnableDataPermission())
        {
            await next(context);
        }
    }
}
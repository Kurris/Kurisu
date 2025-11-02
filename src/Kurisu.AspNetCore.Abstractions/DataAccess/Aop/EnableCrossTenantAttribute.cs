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
    private readonly Type[] _ignoreTypes;

    /// <summary>
    /// ctor
    /// </summary>
    public EnableCrossTenantAttribute()
    {
        _ignoreTypes = [];
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public EnableCrossTenantAttribute(params Type[] ignoreTypes)
    {
        _ignoreTypes = ignoreTypes;
    }

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        await dbContext.IgnoreTenantAsync(async () =>
        {
            await dbContext.EnableCrossTenantAsync(_ignoreTypes,
                async () => { await next(context); });
        });
    }
}
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.Extensions.SqlSugar.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Aop;

/// <summary>
/// 根据权限跨租户
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CrossTenantAttribute : AbstractInterceptorAttribute
{
    private readonly Type[] _ignoreTypes;

    /// <summary>
    /// ctor
    /// </summary>
    public CrossTenantAttribute()
    {
        _ignoreTypes = Array.Empty<Type>();
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public CrossTenantAttribute(params Type[] ignoreTypes)
    {
        _ignoreTypes = ignoreTypes;
    }

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var setting = context.ServiceProvider.GetRequiredService<IQueryableSetting>();
        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();

        await dbContext.IgnoreTenantAsync(async () =>
        {
            setting.EnableCrossTenant = true;
            setting.CrossTenantIgnoreTypes = _ignoreTypes ?? Array.Empty<Type>();
            await next(context);
        });
    }
}
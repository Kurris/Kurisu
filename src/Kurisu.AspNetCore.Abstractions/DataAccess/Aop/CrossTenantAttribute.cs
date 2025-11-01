using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

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
        var setting = context.ServiceProvider.GetRequiredService<ScopeQuerySetting>();

        try
        {
            setting.IgnoreTenant = true;
            setting.EnableCrossTenant = true;
            setting.CrossTenantIgnoreTypes = _ignoreTypes ?? Array.Empty<Type>();
            await next(context);
        }
        finally
        {
            setting.IgnoreTenant = false;
            setting.EnableCrossTenant = false;
            setting.CrossTenantIgnoreTypes = Array.Empty<Type>();
        }
    }
}
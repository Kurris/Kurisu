using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;

/// <summary>
/// 根据权限跨基地
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CrossTenantAttribute : Attribute, IAsyncActionFilter
{
    private readonly Type[] _ignoreTypes;

    /// <summary>
    /// ctor
    /// </summary>
    public CrossTenantAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public CrossTenantAttribute(params Type[] ignoreTypes)
    {
        _ignoreTypes = ignoreTypes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var setting = context.HttpContext.RequestServices.GetService<IQueryableSetting>();
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IDbContext>();

        await dbContext.IgnoreTenantAsync(async () =>
        {
            setting.EnableCrossTenant = true;
            setting.CrossTenantIgnoreTypes = _ignoreTypes ?? Array.Empty<Type>();
            await next();
        });
    }
}
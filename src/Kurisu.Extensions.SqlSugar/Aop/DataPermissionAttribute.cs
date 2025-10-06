using Kurisu.Extensions.SqlSugar.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Aop;

/// <summary>
/// 使用数据权限
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DataPermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly Type[] _ignoreTypes;

    /// <summary>
    /// 数据权限
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public DataPermissionAttribute(params Type[] ignoreTypes)
    {
        _ignoreTypes = ignoreTypes;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var setting = context.HttpContext.RequestServices.GetService<IQueryableSetting>();

        setting.EnableDataPermission = true;
        setting.DataPermissionIgnoreTypes = _ignoreTypes ?? [];
        await next();
    }
}
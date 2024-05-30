using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;

/// <summary>
/// 使用数据权限
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DataPermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly bool _useSqlWhere;
    private readonly string[] _prefixes;

    /// <summary>
    /// 数据权限,使用IQueryable
    /// </summary>
    /// <param name="prefixs"></param>
    public DataPermissionAttribute(params string[] prefixs) : this(false, prefixs)
    {
    }

    /// <summary>
    /// 数据权限
    /// </summary>
    /// <param name="useSqlWhere"></param>
    /// <param name="prefixs"></param>
    public DataPermissionAttribute(bool useSqlWhere, params string[] prefixs)
    {
        _useSqlWhere = useSqlWhere;
        _prefixes = prefixs ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dp = context.HttpContext.RequestServices.GetService<DataPermissionService>();
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ISqlSugarClient>();
        var options = context.HttpContext!.RequestServices.GetService<ISqlSugarOptionsService>();

        try
        {
            dbContext.QueryFilter.ClearAndBackup<ITenantId>();
            options.IgnoreTenant = true;

            dp.Enable = true;
            dp.UseSqlWhere = _useSqlWhere;
            if (dp.UseSqlWhere)
            {
                var currentUser = context.HttpContext.RequestServices.GetService<ICurrentUser>();
                var tenantIds = currentUser.GetUserClaim("tenants")?.Split(',').Select(x => $"'{x}'") ?? Array.Empty<string>();

                if (_prefixes.Any())
                {
                    dp.Wheres.AddRange(_prefixes.ToList().Select(x => $"{x}.`TenantId` in ({string.Join(",", tenantIds)})"));
                }
                else
                {
                    dp.Wheres.Add($"`TenantId` in ({string.Join(",", tenantIds)})");
                }
            }

            await next();

        }
        finally
        {
            options.IgnoreTenant = false;
            dbContext.QueryFilter.Restore();
        }
    }
}
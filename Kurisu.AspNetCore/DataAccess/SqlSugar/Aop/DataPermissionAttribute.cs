using System;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.Core.DataAccess.Entity;
using Kurisu.Core.User.Abstractions;
using Kurisu.SqlSugar.Services;
using Kurisu.SqlSugar.Services.Implements;
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
    private readonly string[] _prefixs;

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
        _prefixs = prefixs;
        if (_prefixs == null)
        {
            _prefixs = Array.Empty<string>();
        }
    }


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
            if (dp.UseSqlWhere == true)
            {
                var currentUser = context.HttpContext.RequestServices.GetService<ICurrentUser>();
                var tenantIds = currentUser.GetUserClaim("tenants")?.Split(',').Select(x => $"'{x}'") ?? Array.Empty<string>();

                if (_prefixs.Any())
                {
                    dp.Wheres.AddRange(_prefixs.ToList().Select(x => $"{x}.`TenantId` in ({string.Join(",", tenantIds)})"));
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
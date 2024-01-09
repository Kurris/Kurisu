using Kurisu.Core.DataAccess.Entity;
using Kurisu.Core.Proxy.Abstractions;
using Kurisu.SqlSugar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.SqlSugar.Aops;

/// <summary>
/// 忽略租户
/// </summary>
public class IgnoreTenant : BaseSqlSugarAop
{
    public IgnoreTenant(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        var options = Accessor.HttpContext!.RequestServices.GetService<ISqlSugarOptionsService>();
        var db = Db;
        try
        {
            db.QueryFilter.ClearAndBackup<ITenantId>();
            options.IgnoreTenant = true;
            await proceed.Invoke(invocation);
        }
        finally
        {
            options.IgnoreTenant = false;
            db.QueryFilter.Restore();
        }
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        var options = Accessor.HttpContext!.RequestServices.GetService<ISqlSugarOptionsService>();
        var db = Db;
        try
        {
            db.QueryFilter.ClearAndBackup<ITenantId>();
            options.IgnoreTenant = true;
            return await proceed.Invoke(invocation);
        }
        finally
        {
            options.IgnoreTenant = false;
            db.QueryFilter.Restore();
        }
    }
}
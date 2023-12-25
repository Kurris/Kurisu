using Kurisu.Core.DataAccess.Entity;
using Kurisu.Core.Proxy.Abstractions;
using Microsoft.AspNetCore.Http;

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
        var db = Db;
        try
        {
            db.QueryFilter.ClearAndBackup<ITenantId>();
            await proceed.Invoke(invocation);
        }
        finally
        {
            db.QueryFilter.Restore();
        }
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        var db = Db;
        try
        {
            db.QueryFilter.ClearAndBackup<ITenantId>();
            return await proceed.Invoke(invocation);
        }
        finally
        {
            db.QueryFilter.Restore();
        }
    }
}
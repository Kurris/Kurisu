using Kurisu.Core.Proxy.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.SqlSugar.Aops;

/// <summary>
/// 忽略所有查询过滤
/// </summary>
public class IgnoreAllQueryFilter : BaseSqlSugarAop
{
    public IgnoreAllQueryFilter(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        var db = Db;
        try
        {
            db.QueryFilter.ClearAndBackup();
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
            db.QueryFilter.ClearAndBackup();
            return await proceed.Invoke(invocation);
        }
        finally
        {
            db.QueryFilter.Restore();
        }
    }
}

using Kurisu.Core.Proxy.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.SqlSugar.Aops;

/// <summary>
/// Db事务
/// </summary>
public class Transactional : BaseSqlSugarAop
{
    public Transactional(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        var db = Db;
        if (db.Ado.IsNoTran())
        {
            await db.Ado.BeginTranAsync();
            try
            {
                await proceed.Invoke(invocation);
                await db.Ado.CommitTranAsync();
            }
            catch (Exception)
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
        }
        else
        {
            await proceed.Invoke(invocation);
        }
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        var db = Db;
        if (db.Ado.IsNoTran())
        {
            await db.Ado.BeginTranAsync();
            try
            {
                var result = await proceed.Invoke(invocation);
                await db.Ado.CommitTranAsync();

                return result;
            }
            catch (Exception)
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
        }
        else
        {
            return await proceed.Invoke(invocation);
        }
    }
}
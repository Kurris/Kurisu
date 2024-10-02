using System;
using System.Data;
using System.Threading.Tasks;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;

/// <summary>
/// 事务
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TransactionalAttribute : Attribute, IAsyncActionFilter
{
    private IsolationLevel? _isolationLevel;

    /// <summary>
    /// 开启事务
    /// </summary>
    public TransactionalAttribute()
    {
    }

    /// <summary>
    /// 开启事务
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    public TransactionalAttribute(IsolationLevel isolationLevel)
    {
        _isolationLevel = isolationLevel;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ISqlSugarClient>();
        _isolationLevel ??= context.HttpContext.RequestServices.GetService<IIsolationLevelService>().Get();
        await dbContext.Ado.BeginTranAsync(_isolationLevel.Value);
        try
        {
            var result = await next();
            if (result.Exception != null)
            {
                throw result.Exception;
            }

            await dbContext.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await dbContext.Ado.RollbackTranAsync();
            throw;
        }
    }
}
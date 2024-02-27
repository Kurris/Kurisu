using System;
using System.Threading.Tasks;
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
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ISqlSugarClient>();
        await dbContext.Ado.BeginTranAsync();
        try
        {
            var result = await next();
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                await dbContext.Ado.CommitTranAsync();
            }
        }
        catch (Exception)
        {
            await dbContext.Ado.RollbackTranAsync();
            throw;
        }
    }
}

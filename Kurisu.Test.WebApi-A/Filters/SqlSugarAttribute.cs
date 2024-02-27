using Kurisu.Core.DataAccess.Entity;
using Kurisu.SqlSugar.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using SqlSugar;

namespace Kurisu.Test.WebApi_A.Filters;

/// <summary>
/// 日志
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class LogAttribute : Attribute, IAsyncActionFilter
{
    public LogAttribute(string title)
    {
        Title = title;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 是否处理差异
    /// </summary>
    public bool Diff { get; set; }


    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        PreSetting(context.HttpContext);
        await next();
    }

    private void PreSetting(HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetService<ISqlSugarOptionsService>();

        options.Title = Title;
        options.Diff = Diff;
        options.BatchNo = Guid.NewGuid();
        options.RoutePath = httpContext.Request.Path.Value;
        options.RaiseTime = DateTime.Now;
    }
}

/// <summary>
/// 事务
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
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

/// <summary>
/// 忽略租户
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreTenantAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ISqlSugarClient>();
        var options = context.HttpContext!.RequestServices.GetService<ISqlSugarOptionsService>();
        try
        {
            dbContext.QueryFilter.ClearAndBackup<ITenantId>();
            options.IgnoreTenant = true;
            await next();
        }
        finally
        {
            options.IgnoreTenant = false;
            dbContext.QueryFilter.Restore();
        }
    }
}



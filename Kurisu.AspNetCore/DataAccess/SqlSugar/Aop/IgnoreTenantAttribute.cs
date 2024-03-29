﻿using System;
using System.Threading.Tasks;
using Kurisu.Core.DataAccess.Entity;
using Kurisu.SqlSugar.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Aop;


/// <summary>
/// 忽略租户
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
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


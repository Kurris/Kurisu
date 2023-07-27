using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.Attributes;

/// <summary>
/// 工作单元
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UnitOfWorkAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// 工作单元
    /// </summary>
    public UnitOfWorkAttribute()
    {
    }

    /// <summary>
    /// 是否开启事务
    /// </summary>
    /// <remarks>
    /// 默认:true
    /// </remarks>
    public bool IsUseTransaction { get; set; } = true;

    /// <summary>
    /// 是否自动提交
    /// </summary>
    /// <remarks>
    /// 默认:true
    /// </remarks>
    public bool IsAutomaticSaveChanges { get; set; } = true;

    /// <summary>
    /// 当事务保存成功后,接受数据库的更改
    /// </summary>
    /// <remarks>
    /// 默认:true
    /// </remarks>
    public bool IsAcceptAllChangesOnSuccess { get; set; } = true;

    /// <summary>
    /// 接口过滤器
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dbContextContainer = context.HttpContext.RequestServices.GetService<IDbContextContainer>()
            ?? throw new NotSupportedException($"工作单元未开启:[{nameof(UnitOfWorkAttribute)}]");

        //检查上下文个数,开启事务管理
        if (dbContextContainer.Count > 0)
        {
            dbContextContainer.IsAutomaticSaveChanges = IsAutomaticSaveChanges;

            if (IsUseTransaction)
            {
                //开启事务
                await dbContextContainer.BeginTransactionAsync();

                //执行请求scope并获取结果
                var result = await next();

                //提交事务
                await dbContextContainer.CommitTransactionAsync(IsAcceptAllChangesOnSuccess, result.Exception);
            }
            else
            {
                await next();

                if (IsAutomaticSaveChanges)
                    await dbContextContainer.SaveChangesAsync(IsAcceptAllChangesOnSuccess);
            }
        }
        else
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<UnitOfWorkAttribute>>()!;
            logger.LogInformation("数据库上下文管理数:{Count}", dbContextContainer.Count);
            await next();
        }
    }
}
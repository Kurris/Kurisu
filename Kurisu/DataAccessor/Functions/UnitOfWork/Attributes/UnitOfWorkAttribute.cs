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
    private readonly bool _isAutomaticSaveChanges;

    /// <summary>
    /// 工作单元,自动提交
    /// </summary>
    public UnitOfWorkAttribute() : this(true)
    {
    }

    /// <summary>
    /// 工作单元
    /// </summary>
    /// <param name="isAutomaticSaveChanges">是否自动提交</param>
    public UnitOfWorkAttribute(bool isAutomaticSaveChanges)
    {
        _isAutomaticSaveChanges = isAutomaticSaveChanges;
    }

    /// <summary>
    /// 当事务保存成功后,接受数据库的更改
    /// </summary>
    public bool AcceptAllChangesOnSuccess { get; set; } = true;

    /// <summary>
    /// 接口过滤器
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var dbContextContainer = context.HttpContext.RequestServices.GetRequiredService<IDbContextContainer>();
        //检查上下文个数,开启事务管理
        if (dbContextContainer.Count > 0)
        {
            dbContextContainer.IsAutomaticSaveChanges = _isAutomaticSaveChanges;

            //开启事务
            await dbContextContainer.BeginTransactionAsync();

            //执行请求scope并获取结果
            var result = await next();

            //提交事务
            await dbContextContainer.CommitTransactionAsync(AcceptAllChangesOnSuccess, result.Exception);
        }
        else
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<UnitOfWorkAttribute>>()!;
            logger.LogInformation("数据库上下文管理数:{Count}", dbContextContainer.Count);

            await next();
        }
    }
}
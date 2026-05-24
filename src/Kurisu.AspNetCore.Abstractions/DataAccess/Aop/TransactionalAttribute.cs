using System.Data;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 定义事务功能
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : AopAttribute
{
    /// <summary>
    /// 隔离级别
    /// </summary>
    public IsolationLevel? IsolationLevel { get; set; }

    /// <summary>
    ///  匹配的异常类型不触发回滚，事务正常提交。仅接受 <see cref="Exception"/> 的子类型。
    /// </summary>
    public Type NoRollbackFor { get; set; }

    /// <summary>
    /// 事务传播行为
    /// </summary>
    public Propagation Propagation { get; set; } = Propagation.Required;

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (NoRollbackFor != null)
            ValidateNoRollbackForType();

        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        var datasourceManager = dbContext.DatasourceManager;

        using var transactionScope = datasourceManager.CreateTransScope(Propagation, IsolationLevel);

        await transactionScope.BeginAsync();
        try
        {
            await next(context);
            await transactionScope.CommitAsync();
        }
        catch (Exception ex)
        {
            if (NoRollbackFor != null && NoRollbackFor.IsAssignableFrom(ex.GetType()))
            {
                await transactionScope.CommitAsync();
            }
            else
            {
                await transactionScope.RollbackAsync();
            }

            throw;
        }
    }

    private bool _noRollbackForValidated;

    private void ValidateNoRollbackForType()
    {
        if (_noRollbackForValidated)
            return;

        if (!typeof(Exception).IsAssignableFrom(NoRollbackFor))
            throw new InvalidOperationException($"NoRollbackFor 必须是 Exception 的子类型，当前值: {NoRollbackFor.FullName}");

        _noRollbackForValidated = true;
    }
}
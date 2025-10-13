using System.Data;
using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 事务
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : AbstractInterceptorAttribute
{
    /// <summary>
    /// 隔离级别
    /// </summary>
    public IsolationLevel? IsolationLevel { get; set; }

    /// <summary>
    ///  指定不回滚的异常
    /// </summary>
    public Type NoRollbackFor { get; set; }

    /// <summary>
    /// 事务传播行为
    /// </summary>
    public Propagation Propagation { get; set; } = Propagation.Required;

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var mgr = context.ServiceProvider.GetRequiredService<IDatasourceManager>();

        // ReSharper disable once ConvertToUsingDeclaration
        using (var transactionScope = IsolationLevel.HasValue
                   ? mgr.CreateTransScope(Propagation, IsolationLevel.Value)
                   : mgr.CreateTransScope(Propagation))
        {
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
    }
}
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
    public IsolationLevel? IsolationLevel { get; set; }

    public Type NoRollbackFor { get; set; }

    public Propagation Propagation { get; set; } = Propagation.Required;

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var transactionManager = context.ServiceProvider.GetRequiredService<ITransactionManager>();

        // ReSharper disable once ConvertToUsingDeclaration
        using (var scopeManager = IsolationLevel.HasValue
                   ? await transactionManager.BeginAsync(Propagation, IsolationLevel.Value)
                   : await transactionManager.BeginAsync(Propagation))
        {
            try
            {
                await next(context);
                await scopeManager.CommitAsync();
            }
            catch (Exception ex)
            {
                if (NoRollbackFor != null && NoRollbackFor.IsAssignableFrom(ex.GetType()))
                {
                    await scopeManager.CommitAsync();
                }
                else
                {
                    await scopeManager.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
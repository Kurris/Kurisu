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
        var mgr = context.ServiceProvider.GetRequiredService<IDatasourceManager>();

        // ReSharper disable once ConvertToUsingDeclaration
        using (var transactionScope = IsolationLevel.HasValue
                   ? mgr.CreateScope(Propagation, IsolationLevel.Value)
                   : mgr.CreateScope(Propagation))
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
                    throw;
                }
            }
        }
    }
}
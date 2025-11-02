using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 数据源
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class DatasourceAttribute : AopAttribute
{
    private readonly string _name;

    public DatasourceAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        _name = name;
    }

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var connectionManager = context.ServiceProvider.GetRequiredService<IDbConnectionManager>();
        using (connectionManager.CreateScope(_name))
        {
            await next(context);
        }
    }
}
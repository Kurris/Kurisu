using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 数据源定义特性
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class DatasourceAttribute : AopAttribute
{
    private readonly string _name;

    /// <summary>
    /// 数据源名称
    /// </summary>
    /// <param name="name"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DatasourceAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        _name = name;
    }

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var ctx = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (ctx.CreateDatasourceScope(_name))
        {
            await next(context);
        }
    }
}
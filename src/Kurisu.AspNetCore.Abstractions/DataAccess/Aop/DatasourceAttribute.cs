using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
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
    /// ctor
    /// </summary>
    public DatasourceAttribute()
    {
        _name = string.Empty;
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="name">数据源名称</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DatasourceAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        _name = name;
    }

    /// <summary>
    /// 指定数据源操作
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var ctx = context.ServiceProvider.GetRequiredService<IDbContext>();
        var name = _name;
        if (string.IsNullOrEmpty(name))
        {
            var connectionMgr = context.ServiceProvider.GetService<IDbConnectionStringManager>();
            name = connectionMgr.Current;
        }
        using (ctx.CreateDatasourceScope(name))
        {
            await next(context);
        }
    }
}
using System.Reflection;
using Kurisu.Core.Proxy.Abstractions;
using Kurisu.SqlSugar.Attributes;
using Kurisu.SqlSugar.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.SqlSugar.Aops;

/// <summary>
/// Db操作日志
/// </summary>
public class Log : BaseSqlSugarAop
{
    public Log(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        PreSetting(invocation);
        await proceed(invocation);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        PreSetting(invocation);
        return await proceed.Invoke(invocation);
    }


    private void PreSetting(IProxyInvocation invocation)
    {
        var log = invocation.Method.GetCustomAttribute<LogAttribute>()!;
        var options = Accessor.HttpContext!.RequestServices.GetService<ISqlSugarOptionsService>();

        options.Diff = log.Diff;
        options.BatchNo = Guid.NewGuid();
        options.RoutePath = Accessor.HttpContext.Request.Path.Value;
        options.RaiseTime = DateTime.Now;
    }
}
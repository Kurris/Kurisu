using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>缓存成功完成的 Task&lt;T&gt; 查询结果。应位于鉴权、校验和事务拦截器内层。</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class CacheableAttribute : AopAttribute
{
    public CacheableAttribute(string region)
    {
        Region = region;
        Order = 1000;
    }
    public string Region { get; }
    public string Policy { get; set; } = "Default";
    /// <summary>业务 Key 参数的位置。-1 使用方法签名和全部业务参数；显式指定时可跨方法精确失效。</summary>
    public int KeyParameterIndex { get; set; } = -1;

    public override Task Invoke(AspectContext context, AspectDelegate next)
        => context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>().InvokeAsync(context, next, this);
}

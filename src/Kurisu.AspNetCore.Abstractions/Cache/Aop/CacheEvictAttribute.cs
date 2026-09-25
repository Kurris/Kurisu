using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>方法成功后注册事务提交后精确失效。与查询使用相同区域、策略和业务 Key 类型。</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class CacheEvictAttribute : AopAttribute
{
    public CacheEvictAttribute(string region)
    {
        Region = region;
        Order = 1000;
    }
    public string Region { get; }
    public string Policy { get; set; } = "Default";
    public int KeyParameterIndex { get; set; } = 0;

    public override Task Invoke(AspectContext context, AspectDelegate next)
        => context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>().EvictAsync(context, next, this);
}

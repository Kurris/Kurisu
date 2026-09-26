using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 方法成功后注册事务提交后精确失效。与查询使用相同区域、策略和业务 Key 类型。
/// 表达式统一使用服务方法的参数名；接口代理使用接口参数名，与特性声明位置无关。
/// </summary>
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
    /// <summary>单个业务 Key 表达式，与 Keys 必须且只能指定一个。</summary>
    public string Key { get; set; }
    /// <summary>多个业务 Key 的集合表达式，例如 inputs.Select(x =&gt; x.Id)。</summary>
    public string Keys { get; set; }
    /// <summary>方法执行前求值的布尔表达式；为 false 时不注册失效回调。</summary>
    public string Condition { get; set; }

    public override Task Invoke(AspectContext context, AspectDelegate next)
        => context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>().EvictAsync(context, next, this);
}

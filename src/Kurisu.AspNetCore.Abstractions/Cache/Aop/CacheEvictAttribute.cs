using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Aop;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 方法执行前获取缓存加载锁，成功后批量失效并释放锁。与查询使用相同区域、版本及规范化业务 Key。
/// 不管理事务或等待外层提交；事务提交应由开发者安排在被拦截方法完成之前。
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

    /// <summary>缓存区域，与查询使用相同区域。</summary>
    public string Region { get; }

    /// <summary>命名策略名称；未指定或名称未命中时使用 MethodCacheOptions.Default。</summary>
    public string Policy { get; set; }

    /// <summary>单个业务 Key 表达式；与 Keys 均省略时取首参数，仅支持 string、int、long。不能与 Keys 同时设置。</summary>
    public string Key { get; set; }

    /// <summary>多个业务 Key 的集合表达式，例如 inputs.Select(x =&gt; x.Id)，每项须为 string、int、long，逐项生成 Key 并去重。不能与 Key 同时设置。</summary>
    public string Keys { get; set; }

    /// <summary>方法执行前求值的布尔表达式；为 false 时仍执行业务，但不获取缓存锁或删除缓存。</summary>
    public string Condition { get; set; }

    public override Task Invoke(AspectContext context, AspectDelegate next)
        => context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>().EvictAsync(context, next, this);
}
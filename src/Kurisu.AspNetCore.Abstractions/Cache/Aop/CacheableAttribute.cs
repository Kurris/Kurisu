using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 仅支持返回 Task&lt;T&gt; 的查询方法，缓存成功完成后的结果。
/// 不支持 Pagination&lt;T&gt; 及其派生类型的分页返回值。
/// 跨租户、忽略租户或软删除过滤、数据权限及分表等特殊查询不应使用此特性。
/// 表达式统一使用服务方法的参数名；接口代理使用接口参数名，与特性声明位置无关。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class CacheableAttribute(string region) : AopAttribute
{
    public string Region { get; } = region;
    public string Policy { get; set; } = "Default";

    /// <summary>业务 Key 表达式，例如 id 或 input.Id；省略时使用方法签名和全部业务参数。</summary>
    public string Key { get; set; }

    public override Task Invoke(AspectContext context, AspectDelegate next)
    {
        var executor = context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>();
        return executor.InvokeAsync(context, next, this);
    }
}

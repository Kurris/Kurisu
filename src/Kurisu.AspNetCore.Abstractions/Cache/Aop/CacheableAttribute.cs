using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Aop;
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
    /// <summary>缓存区域；同一区域应对应相同的结果结构和查询语义。</summary>
    public string Region => region;

    /// <summary>命名策略名称；未指定或名称未命中时使用 MethodCacheOptions.Default。</summary>
    public string Policy { get; set; }

    /// <summary>业务 Key 表达式，例如 id 或 input.Id；省略时取首参数。仅支持 string、int、long，其他参数不参与 Key；多参数查询按需显式组合字符串。</summary>
    public string Key { get; set; }

    public override Task Invoke(AspectContext context, AspectDelegate next)
    {
        var executor = context.ServiceProvider.GetRequiredService<IMethodCacheExecutor>();
        return executor.InvokeAsync(context, next, this);
    }
}
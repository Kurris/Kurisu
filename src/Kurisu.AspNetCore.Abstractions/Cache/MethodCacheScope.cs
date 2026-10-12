namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>每次调用独立创建。业务可通过贡献器补充权限版本、数据源等影响结果的维度。</summary>
public sealed class MethodCacheScope
{
    /// <summary>按名称排序的上下文维度；默认 Key 生成器接受 string、int、long 或 null，复杂维度应先转换为业务字符串。</summary>
    public IDictionary<string, object> Dimensions { get; } = new SortedDictionary<string, object>(StringComparer.Ordinal);

    public bool BypassCache { get; set; }
}

public interface IMethodCacheScopeContributor
{
    void Contribute(MethodCacheScope scope);
}
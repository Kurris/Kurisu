namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>每次调用独立创建。业务可通过贡献器补充权限版本、数据源等影响结果的维度。</summary>
public sealed class MethodCacheScope
{
    public IDictionary<string, object> Dimensions { get; } = new SortedDictionary<string, object>(StringComparer.Ordinal);
    public bool BypassCache { get; set; }
}

public interface IMethodCacheScopeContributor
{
    void Contribute(MethodCacheScope scope);
}

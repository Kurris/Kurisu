using AspectCore.DynamicProxy;

namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ICacheKeyGenerator
{
    /// <summary>根据首参数生成缓存标识，仅支持 string、int、long；其余参数不参与 Key，相同区域和 Key 可跨方法共享。</summary>
    string Generate(AspectContext context, string region, MethodCachePolicy policy, MethodCacheScope scope);

    /// <summary>根据表达式求值后的业务 Key 生成缓存标识，仅支持 string、int、long，统一转为字符串并转义。</summary>
    string Generate(string region, object value, MethodCachePolicy policy, MethodCacheScope scope);
}
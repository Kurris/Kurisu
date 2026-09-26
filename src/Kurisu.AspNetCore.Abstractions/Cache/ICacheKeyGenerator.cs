using AspectCore.DynamicProxy;

namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ICacheKeyGenerator
{
    string Generate(AspectContext context, string region, MethodCachePolicy policy, MethodCacheScope scope);

    /// <summary>根据表达式求值后的业务 Key 生成跨方法共享的缓存标识。</summary>
    string Generate(string region, object value, MethodCachePolicy policy, MethodCacheScope scope);
}
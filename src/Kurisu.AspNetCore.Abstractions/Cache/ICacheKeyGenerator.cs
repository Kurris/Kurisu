using AspectCore.DynamicProxy;

namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface ICacheKeyGenerator
{
    string Generate(AspectContext context, string region, int keyParameterIndex,
        MethodCachePolicy policy, MethodCacheScope scope);
}

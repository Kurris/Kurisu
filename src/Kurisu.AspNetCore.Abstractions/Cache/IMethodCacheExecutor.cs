using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;

namespace Kurisu.AspNetCore.Abstractions.Cache;

public interface IMethodCacheExecutor
{
    Task InvokeAsync(AspectContext context, AspectDelegate next, CacheableAttribute attribute);
    Task EvictAsync(AspectContext context, AspectDelegate next, CacheEvictAttribute attribute);
}

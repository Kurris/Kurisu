using Kurisu.Expressions;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache.MethodCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.Cache;

public static class MethodCacheServiceCollectionExtensions
{
    /// <summary>注册方法缓存。另需注册 ICache、ILockable，并使用 Kurisu 动态代理容器。</summary>
    public static IServiceCollection AddMethodCaching(this IServiceCollection services, Action<MethodCacheOptions> configure = null)
    {
        var builder = services.AddOptions<MethodCacheOptions>();
        if (configure != null) builder.Configure(configure);
        builder.Validate(o => !string.IsNullOrWhiteSpace(o.KeyPrefix) && o.Policies.Count > 0 &&
            o.Policies.All(p => !string.IsNullOrWhiteSpace(p.Key) && IsValid(p.Value)), "方法缓存配置无效。");
        services.TryAddSingleton<ExpressionCompiler>();
        services.TryAddSingleton<MethodExpressionEvaluator>();
        services.TryAddSingleton<MethodCacheCoordinator>();
        services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.TryAddScoped<IMethodCacheExecutor, MethodCacheExecutor>();
        return services;
    }

    private static bool IsValid(MethodCachePolicy policy) => policy != null &&
        !string.IsNullOrWhiteSpace(policy.Version) &&
        policy.Expiry > TimeSpan.Zero &&
        ValidTimer(policy.LockWaitTimeout) && ValidTimer(policy.LockPollInterval) &&
        ValidTimer(policy.LockExpiry) && ValidTimer(policy.QueryTimeout);

    private static bool ValidTimer(TimeSpan value) => value > TimeSpan.Zero && value.TotalMilliseconds <= int.MaxValue;
}

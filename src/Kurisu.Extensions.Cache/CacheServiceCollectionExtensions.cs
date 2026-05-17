using System.IO;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache;

/// <summary>
/// cache
/// </summary>
public static class CacheServiceCollectionExtensions
{

    /// <summary>
    /// 添加redis服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="log"></param>
    /// <param name="isSentinel"></param>
    /// <returns></returns>
    public static IServiceCollection AddRedis(this IServiceCollection services, TextWriter log = null, bool isSentinel = false)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value
                ?? throw new InvalidOperationException("RedisOptions 未配置。");

            if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            {
                throw new InvalidOperationException("RedisOptions.ConnectionString 未配置。");
            }

            return !isSentinel
                ? ConnectionMultiplexer.Connect(redisOptions.ConnectionString, log)
                : ConnectionMultiplexer.SentinelConnect(redisOptions.ConnectionString, log);
        });

        services.AddSingleton<RedisCache>();
        services.TryAddSingleton<ILockable>(sp => sp.GetRequiredService<RedisCache>());
        services.TryAddSingleton<ICache>(sp => sp.GetRequiredService<RedisCache>());
        return services;
    }
}

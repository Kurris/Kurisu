using System.IO;
using Kurisu.AspNetCore.Cache.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.Cache.Extensions;

/// <summary>
/// cache
/// </summary>
public static class CacheServiceCollectionExtensions
{
    /// <summary>
    /// 添加缓存
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.TryAddSingleton<ICache, CommonCache>();
        return services;
    }

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
            var redisOptions = sp.GetService<IOptions<RedisOptions>>().Value;

            return !isSentinel
                ? ConnectionMultiplexer.Connect(redisOptions.ConnectionString, log)
                : ConnectionMultiplexer.SentinelConnect(redisOptions.ConnectionString, log);
        });

        services.AddSingleton<RedisCache>();
        return services;
    }
}
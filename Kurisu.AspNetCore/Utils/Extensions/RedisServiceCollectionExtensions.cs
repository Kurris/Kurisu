using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// redis ioc注入扩展
/// </summary>
public static class RedisServiceCollectionExtensions
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
            var redisOptions = sp.GetService<IOptions<RedisOptions>>().Value;

            if (!isSentinel)
            {
                return ConnectionMultiplexer.Connect(redisOptions.ConnectionString, log);
            }

            return ConnectionMultiplexer.SentinelConnect(redisOptions.ConnectionString, log);
        });

        services.AddSingleton<RedisCache>();

        return services;
    }
}

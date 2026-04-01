using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace Kurisu.Extensions.DataProtection.Redis;

public static class RedisDataProtectionServiceCollectionExtensions
{
    /// <summary>
    /// 添加Redis作为数据保护密钥存储
    /// </summary>
    /// <param name="services"></param>
    /// <param name="applicationName"></param>
    /// <param name="redisConnectionString"></param>
    /// <returns></returns>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, string applicationName, string redisConnectionString)
    {
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        return builder.SetApplicationName(applicationName).PersistKeysToStackExchangeRedis(redisConnection);
    }
}

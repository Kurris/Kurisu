using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Cache;

internal static class RedisCacheTestSupport
{
    public static ServiceProvider BuildServiceProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("环境变量 KURISU_TEST_REDIS 未设置。请设置 Redis 连接字符串后重试，例如: localhost:6379");
        }

        return BuildServiceProvider(connectionString);
    }

    public static ServiceProvider BuildServiceProvider(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<RedisOptions>().Configure(options => options.ConnectionString = connectionString);
        services.AddRedis();
        return services.BuildServiceProvider();
    }

    public static DistributedLockAcquisitionOptions BuildLockOptions(
        TimeSpan? expiry = null, int retryCount = 3, bool enableRetry = true)
    {
        return new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = expiry.HasValue
                ? LockTimeModeHandler.FixedExpiry(expiry)
                : LockTimeModeHandler.InfiniteRenewal(),
            RetryStrategy = enableRetry
                ? new DefaultLockRetryStrategy(retryCount)
                : new DefaultLockRetryStrategy(0)
        };
    }
}

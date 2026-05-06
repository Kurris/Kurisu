using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Cache;

internal static class RedisCacheTestSupport
{
    public static ServiceProvider BuildServiceProvider()
    {
        const string fallbackConnectionString = "192.168.199.124:6379,defaultDatabase=1,password=dlhis123";
        var connectionString = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = fallbackConnectionString;
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

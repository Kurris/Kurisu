using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "data-operations")]
public class RedisCacheDataOperationTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS")
            ?? throw new InvalidOperationException("环境变量 KURISU_TEST_REDIS 未设置");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<RedisOptions>().Configure(o => o.ConnectionString = connectionString);
        services.AddRedis();
        return services.BuildServiceProvider();
    }

    [Fact(DisplayName = "SetAsync后GetAsync应返回相同值")]
    public async Task SetAsync_GetAsync_ShouldRoundtrip()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:roundtrip:{Guid.NewGuid():N}";
        var expected = new TestDto { Name = "hello", Value = 42 };

        await cache.SetAsync(key, expected);
        var actual = await cache.GetAsync<TestDto>(key);

        Assert.NotNull(actual);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Value, actual.Value);

        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "SetAsync设置过期后GetAsync应返回默认值")]
    public async Task SetAsync_WithExpiry_ShouldExpire()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:expire:{Guid.NewGuid():N}";

        await cache.SetAsync(key, "transient-data", TimeSpan.FromMilliseconds(200));
        Assert.True(await cache.ExistsAsync(key));

        await Task.Delay(500);
        Assert.False(await cache.ExistsAsync(key));
        var result = await cache.GetAsync<string>(key);
        Assert.Null(result);
    }

    [Fact(DisplayName = "RemoveAsync应删除已存在的Key")]
    public async Task RemoveAsync_ShouldDeleteExistingKey()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:remove:{Guid.NewGuid():N}";

        await cache.SetAsync(key, "data");
        Assert.True(await cache.ExistsAsync(key));

        await cache.RemoveAsync(key);
        Assert.False(await cache.ExistsAsync(key));
    }

    [Fact(DisplayName = "RemoveAsync对不存在的Key应返回false")]
    public async Task RemoveAsync_ShouldReturnFalse_WhenKeyNotExists()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:remove-missing:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(key);

        var result = await cache.RemoveAsync(key);
        Assert.False(result);
    }

    [Fact(DisplayName = "ExistsAsync对存在的Key应返回true")]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:exists:{Guid.NewGuid():N}";

        await cache.SetAsync(key, "data");
        Assert.True(await cache.ExistsAsync(key));

        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "ExistsAsync对不存在的Key应返回false")]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyNotExists()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:exists-missing:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(key);

        Assert.False(await cache.ExistsAsync(key));
    }

    [Fact(DisplayName = "GetAsync对不存在的Key应返回默认值")]
    public async Task GetAsync_ShouldReturnDefault_WhenKeyNotExists()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:get-missing:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(key);

        var result = await cache.GetAsync<TestDto>(key);
        Assert.Null(result);
    }

    [Fact(DisplayName = "SetAsync覆盖已有Key应成功")]
    public async Task SetAsync_ShouldOverwrite_WhenKeyAlreadyExists()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:overwrite:{Guid.NewGuid():N}";

        await cache.SetAsync(key, "original");
        await cache.SetAsync(key, "updated");

        var result = await cache.GetAsync<string>(key);
        Assert.Equal("updated", result);

        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "SetAsync传入null值应序列化null")]
    public async Task SetAsync_ShouldStoreNull()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:null-value:{Guid.NewGuid():N}";

        await cache.SetAsync<string?>(key, null);
        Assert.True(await cache.ExistsAsync(key));

        var result = await cache.GetAsync<string?>(key);
        Assert.Null(result);

        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "SetAsync无过期应持久化")]
    public async Task SetAsync_WithoutExpiry_ShouldPersist()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:persist:{Guid.NewGuid():N}";

        await cache.SetAsync(key, "persistent-data");
        await Task.Delay(500);
        Assert.True(await cache.ExistsAsync(key));

        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "GetAsync反序列化复杂对象应正确")]
    public async Task GetAsync_ShouldDeserializeComplexObject()
    {
        using var serviceProvider = BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var key = $"test:complex:{Guid.NewGuid():N}";
        var expected = new TestDto
        {
            Name = "complex",
            Value = 999,
            Tags = new List<string> { "a", "b", "c" },
            Nested = new TestDto { Name = "inner", Value = 42 }
        };

        await cache.SetAsync(key, expected);
        var actual = await cache.GetAsync<TestDto>(key);

        Assert.NotNull(actual);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Value, actual.Value);
        Assert.Equal(expected.Tags, actual.Tags);
        Assert.NotNull(actual.Nested);
        Assert.Equal(expected.Nested.Name, actual.Nested.Name);

        await cache.RemoveAsync(key);
    }
}

public class TestDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public List<string>? Tags { get; set; }
    public TestDto? Nested { get; set; }
}

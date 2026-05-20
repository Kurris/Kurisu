using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "all-operations")]
public class RedisCacheAllOperationsTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        var cs = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS")
            ?? throw new InvalidOperationException("KURISU_TEST_REDIS 未设置");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<RedisOptions>().Configure(o => o.ConnectionString = cs);
        services.AddRedis();
        return services.BuildServiceProvider();
    }

    #region String 同步操作

    [Fact(DisplayName = "StringSet+StringGet同步基础读写")]
    public void StringSet_StringGet_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:sync:{Guid.NewGuid():N}";
        cache.StringSet(key, "sync-val");
        Assert.Equal("sync-val", cache.StringGet(key));
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "StringSet带When同步条件写入")]
    public void StringSet_WithWhen_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:when:{Guid.NewGuid():N}";
        Assert.True(cache.StringSet(key, "v1", null, StackExchange.Redis.When.NotExists));
        Assert.False(cache.StringSet(key, "v2", null, StackExchange.Redis.When.NotExists));
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "SetAsync泛型+GetAsync泛型序列化往返")]
    public async Task SetAsync_GetAsync_Generic()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:gen:{Guid.NewGuid():N}";
        await cache.SetAsync(key, new ValObj { N = 2, S = "sync" });
        var r = await cache.GetAsync<ValObj>(key);
        Assert.NotNull(r);
        Assert.Equal(2, r.N);
        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "GetAsync泛型在Key不存在时返回默认值")]
    public async Task GetAsync_Generic_ShouldReturnDefault_WhenKeyNotExists()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:missing:{Guid.NewGuid():N}";
        await cache.RemoveAsync(key);

        var result = await cache.GetAsync<ValObj>(key);
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetOrSetAsync缓存命中时直接返回,不调用factory")]
    public async Task GetOrSetAsync_CacheHit_ReturnsCachedValue_DoesNotCallFactory()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:hit:{Guid.NewGuid():N}";
        await cache.SetAsync(key, new ValObj { N = 1, S = "cached" });

        var factoryCalled = false;
        var r = await cache.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new ValObj { N = 99 });
        });
        Assert.False(factoryCalled);
        Assert.Equal(1, r.N);
        Assert.Equal("cached", r.S);
        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "GetOrSetAsync缓存未命中时调用factory并写入缓存")]
    public async Task GetOrSetAsync_CacheMiss_CallsFactory_AndSetsCache()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"s:miss:{Guid.NewGuid():N}";
        await cache.RemoveAsync(key);

        var r = await cache.GetOrSetAsync(key, () => Task.FromResult(new ValObj { N = 2, S = "new" }));
        Assert.Equal(2, r.N);
        Assert.Equal("new", r.S);

        var cached = await cache.GetAsync<ValObj>(key);
        Assert.NotNull(cached);
        Assert.Equal(2, cached.N);
        await cache.RemoveAsync(key);
    }

    [Fact(DisplayName = "StringSet同步批量")]
    public void StringSet_Batch_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var d = new Dictionary<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue> { ["s:ba"] = "va" };
        Assert.True(cache.StringSet(d));
        cache.KeyDelete("s:ba");
    }

    #endregion

    #region String 异步操作

    [Fact(DisplayName = "StringSetAsync+StringGetAsync基础读写")]
    public async Task StringSetAsync_StringGetAsync_Roundtrip()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"str:{Guid.NewGuid():N}";
        await cache.StringSetAsync(key, "hello");
        Assert.Equal("hello", await cache.StringGetAsync(key));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "StringSetAsync带When条件")]
    public async Task StringSetAsync_WithWhen()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"str:when:{Guid.NewGuid():N}";
        Assert.True(await cache.StringSetAsync(key, "v1", null, StackExchange.Redis.When.NotExists));
        Assert.False(await cache.StringSetAsync(key, "v2", null, StackExchange.Redis.When.NotExists));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "StringSetAsync批量写入")]
    public async Task StringSetAsync_Batch()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var d = new Dictionary<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue> { ["a"] = "va" };
        Assert.True(await cache.StringSetAsync(d));
        await cache.KeyDeleteAsync("a");
    }

    #endregion

    #region Hash 同步操作

    [Fact(DisplayName = "HashSet+HashGet+HashExists同步")]
    public void HashSet_HashGet_HashExists_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:sync:{Guid.NewGuid():N}";
        cache.HashSet(key, "f1", "v1");
        Assert.True(cache.HashExists(key, "f1"));
        Assert.Equal("v1", cache.HashGet(key, "f1").ToString());
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "HashDelete同步+HashSet批量+HashGet批量+HashKeys+HashValues")]
    public void HashDelete_Batch_HashSet_Batch_HashKeys_HashValues_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:sync2:{Guid.NewGuid():N}";
        cache.HashSet(key, new[] { new StackExchange.Redis.HashEntry("k1", "v1"), new StackExchange.Redis.HashEntry("k2", "v2") });
        Assert.Equal(2, cache.HashGet(key, new StackExchange.Redis.RedisValue[] { "k1", "k2" }).Length);
        Assert.Equal(2, cache.HashKeys(key).Count());
        Assert.Equal(2, cache.HashValues(key).Length);
        cache.HashDelete(key, "k1");
        Assert.False(cache.HashExists(key, "k1"));
        cache.HashDelete(key, new[] { "k2" });
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "HashSet泛型+HashGet泛型同步")]
    public void HashSet_HashGet_Generic_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:gen:{Guid.NewGuid():N}";
        cache.HashSet(key, "d", new ValObj { N = 8, S = "hs" });
        var r = cache.HashGet<ValObj>(key, "d");
        Assert.NotNull(r);
        Assert.Equal(8, r.N);
        cache.KeyDelete(key);
    }

    #endregion

    #region Hash 异步操作

    [Fact(DisplayName = "HashSetAsync+HashGetAsync+HashExistsAsync基础操作")]
    public async Task HashSetAsync_HashGetAsync_HashExistsAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:{Guid.NewGuid():N}";
        await cache.HashSetAsync(key, "f1", "v1");
        Assert.True(await cache.HashExistsAsync(key, "f1"));
        Assert.Equal("v1", (await cache.HashGetAsync(key, "f1")).ToString());
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "HashDeleteAsync+HashGetAllAsync")]
    public async Task HashDeleteAsync_HashGetAllAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:del:{Guid.NewGuid():N}";
        await cache.HashSetAsync(key, "a", "1");
        await cache.HashSetAsync(key, "b", "2");
        await cache.HashDeleteAsync(key, "a");
        var all = await cache.HashGetAllAsync(key);
        Assert.Single(all);
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "HashDeleteAsync批量删除")]
    public async Task HashDeleteAsync_Batch()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:bd:{Guid.NewGuid():N}";
        await cache.HashSetAsync(key, "x", "1");
        await cache.HashSetAsync(key, "y", "2");
        await cache.HashDeleteAsync(key, new[] { "x", "y" });
        Assert.False(await cache.HashExistsAsync(key, "x"));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "HashSetAsync批量+HashGetAsync批量+HashKeysAsync+HashValuesAsync")]
    public async Task HashSetAsync_Batch_HashGetAsync_Batch_HashKeysAsync_HashValuesAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:all:{Guid.NewGuid():N}";
        await cache.HashSetAsync(key, new[] { new StackExchange.Redis.HashEntry("k1", "v1"), new StackExchange.Redis.HashEntry("k2", "v2") });
        var vals = await cache.HashGetAsync(key, new StackExchange.Redis.RedisValue[] { "k1", "k2" });
        Assert.Equal(2, vals.Count());
        Assert.Contains("k1", (await cache.HashKeysAsync(key)).Select(k => k.ToString()));
        Assert.Contains("v1", (await cache.HashValuesAsync(key)).Select(v => v.ToString()));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "HashSetAsync泛型+HashGetAsync泛型")]
    public async Task HashSetAsync_HashGetAsync_Generic()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"h:gen:{Guid.NewGuid():N}";
        await cache.HashSetAsync(key, "d", new ValObj { N = 9, S = "h" });
        var r = await cache.HashGetAsync<ValObj>(key, "d");
        Assert.NotNull(r);
        Assert.Equal(9, r.N);
        await cache.KeyDeleteAsync(key);
    }

    #endregion

    #region List 同步操作

    [Fact(DisplayName = "ListLeftPush+ListRightPush+ListRange+ListLength同步")]
    public void ListPush_Range_Length_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"l:sync:{Guid.NewGuid():N}";
        cache.ListLeftPush(key, "a");
        cache.ListRightPush(key, "b");
        Assert.Equal(2, cache.ListLength(key));
        Assert.Equal(2, cache.ListRange(key).Count());
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "ListLeftPop+ListRightPop+ListRemove同步")]
    public void ListPop_Remove_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"l:sync2:{Guid.NewGuid():N}";
        cache.ListRightPush(key, "first");
        cache.ListRightPush(key, "last");
        Assert.Equal("first", cache.ListLeftPop(key));
        Assert.Equal("last", cache.ListRightPop(key));
        cache.ListRightPush(key, "x");
        cache.ListRightPush(key, "x");
        Assert.Equal(2, cache.ListRemove(key, "x"));
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "ListPush+ListPop泛型同步序列化全部重载")]
    public void ListPushPop_Generic_Sync_All()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var k1 = $"l:gl:{Guid.NewGuid():N}";
        var k2 = $"l:gr:{Guid.NewGuid():N}";

        // ListLeftPush<T> + ListLeftPop<T>
        cache.ListLeftPush(k1, new ValObj { N = 5, S = "ls" });
        var r1 = cache.ListLeftPop<ValObj>(k1);
        Assert.NotNull(r1);
        Assert.Equal(5, r1.N);

        // ListRightPush<T> + ListRightPop<T>
        cache.ListRightPush(k2, new ValObj { N = 6, S = "rs" });
        var r2 = cache.ListRightPop<ValObj>(k2);
        Assert.NotNull(r2);
        Assert.Equal(6, r2.N);

        cache.KeyDelete(k1);
        cache.KeyDelete(k2);
    }

    #endregion

    #region List 异步操作

    [Fact(DisplayName = "ListLeftPushAsync+ListRightPushAsync+ListRangeAsync+ListLengthAsync")]
    public async Task ListPush_Range_Length()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"l:{Guid.NewGuid():N}";
        await cache.ListLeftPushAsync(key, "a");
        await cache.ListRightPushAsync(key, "b");
        Assert.Equal(2, await cache.ListLengthAsync(key));
        Assert.Equal(2, (await cache.ListRangeAsync(key)).Count());
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "ListLeftPopAsync+ListRightPopAsync")]
    public async Task ListPop()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"l:pop:{Guid.NewGuid():N}";
        await cache.ListRightPushAsync(key, "first");
        await cache.ListRightPushAsync(key, "last");
        Assert.Equal("first", (await cache.ListLeftPopAsync(key)).ToString());
 Assert.Equal("last", (await cache.ListRightPopAsync(key)).ToString());
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "ListRemoveAsync")]
    public async Task ListRemoveAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"l:rem:{Guid.NewGuid():N}";
        await cache.ListRightPushAsync(key, "x");
        await cache.ListRightPushAsync(key, "x");
        Assert.Equal(2, await cache.ListRemoveAsync(key, "x"));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "ListPush+ListPop泛型序列化全重载")]
    public async Task ListPushPop_Generic_All()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var k1 = $"l:ga:{Guid.NewGuid():N}";
        var k2 = $"l:gb:{Guid.NewGuid():N}";

        // ListRightPushAsync<T> + ListRightPopAsync<T>
        await cache.ListRightPushAsync(k1, new ValObj { N = 7, S = "li" });
        var r1 = await cache.ListRightPopAsync<ValObj>(k1);
        Assert.NotNull(r1);
        Assert.Equal(7, r1.N);

        // ListLeftPushAsync<T> + ListLeftPopAsync<T>
        await cache.ListLeftPushAsync(k2, new ValObj { N = 8, S = "li2" });
        var r2 = await cache.ListLeftPopAsync<ValObj>(k2);
        Assert.NotNull(r2);
        Assert.Equal(8, r2.N);

        await cache.KeyDeleteAsync(k1);
        await cache.KeyDeleteAsync(k2);
    }

    #endregion

    #region SortedSet 同步操作

    [Fact(DisplayName = "SortedSet增删查同步")]
    public void SortedSet_All_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"z:sync:{Guid.NewGuid():N}";
        Assert.True(cache.SortedSetAdd(key, "m1", 1.0));
        Assert.True(cache.SortedSetAdd(key, "m2", 2.0));
        Assert.Equal(2, cache.SortedSetLength(key));
        Assert.Equal(2, cache.SortedSetRangeByRank(key).Count());
        Assert.True(cache.SortedSetRemove(key, "m1"));
        cache.KeyDelete(key);
    }

    [Fact(DisplayName = "SortedSetAdd泛型同步")]
    public void SortedSetAdd_Generic_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"z:gen:{Guid.NewGuid():N}";
        Assert.True(cache.SortedSetAdd(key, new ValObj { N = 4 }, 3.0));
        cache.KeyDelete(key);
    }

    #endregion

    #region SortedSet 异步操作

    [Fact(DisplayName = "SortedSetAddAsync+SortedSetRemoveAsync+SortedSetLengthAsync+SortedSetRangeByRankAsync")]
    public async Task SortedSet_AllAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"z:{Guid.NewGuid():N}";
        Assert.True(await cache.SortedSetAddAsync(key, "m1", 1.0));
        Assert.True(await cache.SortedSetAddAsync(key, "m2", 2.0));
        Assert.Equal(2, await cache.SortedSetLengthAsync(key));
        Assert.Equal(2, (await cache.SortedSetRangeByRankAsync(key)).Length);
        Assert.True(await cache.SortedSetRemoveAsync(key, "m1"));
        await cache.KeyDeleteAsync(key);
    }

    [Fact(DisplayName = "SortedSetAddAsync泛型")]
    public async Task SortedSetAddAsync_Generic()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"z:gen:{Guid.NewGuid():N}";
        Assert.True(await cache.SortedSetAddAsync(key, new ValObj { N = 3 }, 5.0));
        await cache.KeyDeleteAsync(key);
    }

    #endregion

    #region Key 同步操作

    [Fact(DisplayName = "KeyDelete+KeyExists+KeyRename+KeyExpire同步")]
    public void Key_All_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var k1 = $"k:s1:{Guid.NewGuid():N}";
        var k2 = $"k:s2:{Guid.NewGuid():N}";
        cache.StringSet(k1, "x");
        Assert.True(cache.KeyExists(k1));
        Assert.True(cache.KeyRename(k1, k2));
        Assert.False(cache.KeyExists(k1));
        Assert.True(cache.KeyExpire(k2, TimeSpan.FromMilliseconds(100)));
        cache.KeyDelete(k2);
    }

    [Fact(DisplayName = "KeyDelete批量同步")]
    public void KeyDelete_Batch_Sync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var keys = new StackExchange.Redis.RedisKey[] { $"k:bd1:{Guid.NewGuid():N}", $"k:bd2:{Guid.NewGuid():N}" };
        cache.StringSet(keys[0], "a");
        cache.StringSet(keys[1], "b");
        Assert.Equal(2, cache.KeyDelete(keys));
    }

    #endregion

    #region Key 异步操作

    [Fact(DisplayName = "KeyRenameAsync")]
    public async Task KeyRenameAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var a = $"k:old:{Guid.NewGuid():N}";
        var b = $"k:new:{Guid.NewGuid():N}";
        await cache.StringSetAsync(a, "x");
        Assert.True(await cache.KeyRenameAsync(a, b));
        Assert.False(await cache.KeyExistsAsync(a));
        await cache.KeyDeleteAsync(b);
    }

    [Fact(DisplayName = "KeyExpireAsync")]
    public async Task KeyExpireAsync()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var key = $"k:exp:{Guid.NewGuid():N}";
        await cache.StringSetAsync(key, "x");
        Assert.True(await cache.KeyExpireAsync(key, TimeSpan.FromMilliseconds(100)));
        await Task.Delay(300);
        Assert.False(await cache.KeyExistsAsync(key));
    }

    [Fact(DisplayName = "KeyDeleteAsync批量")]
    public async Task KeyDeleteAsync_Batch()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var k1 = $"k:ba:{Guid.NewGuid():N}";
        var k2 = $"k:bb:{Guid.NewGuid():N}";
        await cache.StringSetAsync(k1, "a");
        await cache.StringSetAsync(k2, "b");
        Assert.Equal(2, await cache.KeyDeleteAsync(new StackExchange.Redis.RedisKey[] { k1, k2 }));
    }

    #endregion

    #region 发布订阅

    [Fact(DisplayName = "Publish(RedisValue)同步重载")]
    public void Publish_RedisValue()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var channel = $"ch:rv:{Guid.NewGuid():N}";
        var received = new List<string>();
        cache.Subscribe(channel, (c, v) => { lock (received) received.Add(v.ToString()); });
        // 显式调 Publish(string, RedisValue) 重载而非泛型 Publish<T>
        cache.Publish(channel, (StackExchange.Redis.RedisValue)"raw-value");
        Thread.Sleep(50);
        Assert.True(received.Count >= 1);
    }

    [Fact(DisplayName = "SubscribeAsync+PublishAsync泛型")]
    public async Task SubscribeAsync_PublishAsync_Generic()
    {
        using var sp = BuildServiceProvider();
        var cache = sp.GetRequiredService<RedisCache>();
        var channel = $"ch:sa:{Guid.NewGuid():N}";
        var received = new List<string>();
        await cache.SubscribeAsync(channel, (c, v) => { lock (received) received.Add(v); });
        await cache.PublishAsync(channel, "msg1");
        cache.Publish(channel, new ValObj { N = 1, S = "obj" });
        await cache.PublishAsync(channel, new ValObj { N = 2, S = "obj2" });
        await Task.Delay(100);
        Assert.True(received.Count >= 1);
    }

    #endregion

    public class ValObj { public int N { get; set; } public string S { get; set; } = string.Empty; }
}

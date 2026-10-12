using Kurisu.AspNetCore.Abstractions.Cache;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

/// <summary>
/// RedisCache
/// </summary>
public partial class RedisCache : ICache, IDisposable
{
    /// <summary>
    /// 数据库
    /// </summary>
    private readonly IDatabase _db;

    /// <summary>
    /// redis连接对象
    /// </summary>
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private readonly ILogger<RedisCache> _logger;
    private int _disposed;

    /// <summary>
    /// ctor
    /// </summary>
    public RedisCache(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCache> logger)
    {
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        _db = _connectionMultiplexer.GetDatabase();
        AddRegisterEvent();
    }

    public async Task<T> GetAsync<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return DeserializeRedisValue<T>(await _db.StringGetAsync(key));
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = JsonConvert.SerializeObject(value);
        return await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>批量删除；单机一次提交，Cluster 按槽分组并发提交，返回实际删除条目数。</summary>
    public async Task<long> RemoveManyAsync(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var distinctKeys = keys.Distinct(StringComparer.Ordinal).ToArray();
        foreach (var key in distinctKeys) ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (distinctKeys.Length == 0) return 0;

        var redisKeys = distinctKeys.Select(key => (RedisKey)key).ToArray();
        if (_connectionMultiplexer.GetServers().All(server => server.ServerType != ServerType.Cluster))
            return await _db.KeyDeleteAsync(redisKeys);

        // 同槽批量删除；异步委托也将同步抛出的异常捕获到任务中，确保其他组仍会提交。
        var tasks = redisKeys.GroupBy(key => _connectionMultiplexer.GetHashSlot(key))
            .Select(async group => await _db.KeyDeleteAsync(group.ToArray()));
        var removed = await Task.WhenAll(tasks);
        return removed.Sum();
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _db.KeyExistsAsync(key);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var value = await _db.StringGetAsync(key);
        if (!value.IsNull)
        {
            return DeserializeRedisValue<T>(value);
        }

        var result = await factory();
        var json = JsonConvert.SerializeObject(result);
        await _db.StringSetAsync(key, json, expiry);
        return result;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _connectionMultiplexer.ConnectionRestored -= ConnMultiplexer_ConnectionRestored;
        _connectionMultiplexer.ConnectionFailed -= ConnMultiplexer_ConnectionFailed;
        _connectionMultiplexer.ErrorMessage -= ConnMultiplexer_ErrorMessage;
        _connectionMultiplexer.ConfigurationChanged -= ConnMultiplexer_ConfigurationChanged;
        _connectionMultiplexer.HashSlotMoved -= ConnMultiplexer_HashSlotMoved;
        _connectionMultiplexer.InternalError -= ConnMultiplexer_InternalError;
        _connectionMultiplexer.ConfigurationChangedBroadcast -= ConnMultiplexer_ConfigurationChangedBroadcast;
    }

    private static T DeserializeRedisValue<T>(RedisValue value)
    {
        if (value.IsNull)
        {
            return default!;
        }

        return JsonConvert.DeserializeObject<T>(value)!;
    }
}
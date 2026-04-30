using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache.Locking;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

/// <summary>
/// RedisCache
/// </summary>
public class RedisCache : ILockable, ICache
{
    private readonly RedisReentrantLockContext _reentrantLockContext = new();

    /// <summary>
    /// 数据库
    /// </summary>
    private readonly IDatabase _db;

    /// <summary>
    /// redis连接对象
    /// </summary>
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private readonly ILogger<RedisCache> _logger;

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

    /// <summary>
    /// lock
    /// </summary>
    /// <param name="lockKey">key</param>
    /// <param name="options">锁获取参数</param>
    /// <returns></returns>
    public Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);
        ArgumentNullException.ThrowIfNull(options);

        if (_reentrantLockContext.TryEnter(lockKey, out var reentrantHandler))
        {
            _logger.LogDebug("同一请求内复用锁 {lockKey},跳过重复Redis加锁", lockKey);
            return Task.FromResult(reentrantHandler);
        }

        // 先在当前异步调用链初始化作用域容器，确保首次成功后可被同链路后续调用读取。
        var scopes = _reentrantLockContext.EnsureScopes();

        return LockCoreAsync(lockKey, options, scopes);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return JsonConvert.DeserializeObject<T>(await _db.StringGetAsync(key));
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

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _db.KeyExistsAsync(key);
    }

    private async Task<ILockHandler> LockCoreAsync(string lockKey, DistributedLockAcquisitionOptions options, Dictionary<string, RedisReentrantLockContext.LocalLockScope> scopes)
    {
        ArgumentNullException.ThrowIfNull(options);

        var timeSettings = options.TimeModeHandler.Resolve();
        var retryStrategy = options.RetryStrategy;

        var locker = new RedisLock(_logger, _db, lockKey, timeSettings.Expiry, timeSettings.EnableAutoRenewal, timeSettings.MaxRenewalCount);
        var attempt = 0;
        do
        {
            var handler = await locker.LockAsync();

            if (handler.Acquired)
            {
                return _reentrantLockContext.Register(lockKey, handler, scopes);
            }

            attempt++;
            if (!await retryStrategy.ShouldRetryAsync(attempt))
            {
                _reentrantLockContext.ClearIfEmpty(scopes);
                return handler;
            }

            await retryStrategy.DelayBeforeRetryAsync(attempt);

            _logger.LogInformation("获取 {lockKey} 失败 {attempt}，正在重试", lockKey, attempt);
        } while (!locker.Acquired);

        _reentrantLockContext.ClearIfEmpty(scopes);
        return locker;
    }

    #region String 操作

    /// <summary>
    /// 设置key并保存字符串(如果key已存在,则覆盖值)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public bool StringSet(RedisKey key, string value, TimeSpan? expiry = null)
    {
        return _db.StringSet(key, value, expiry);
    }

    /// <summary>
    /// 设置key并保存字符串，可通过 when 控制覆盖策略。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <param name="when">写入条件（如 NotExists 表示仅在键不存在时写入）。</param>
    /// <returns></returns>
    public bool StringSet(RedisKey key, string value, TimeSpan? expiry, When when)
    {
        return _db.StringSet(key, value, expiry, when);
    }


    /// <summary>
    /// 保存一个字符串值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<bool> StringSetAsync(RedisKey key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry);
    }

    /// <summary>
    /// 异步保存一个字符串值，可通过 when 控制覆盖策略。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <param name="when">写入条件（如 NotExists 表示仅在键不存在时写入）。</param>
    /// <returns></returns>
    public async Task<bool> StringSetAsync(RedisKey key, string value, TimeSpan? expiry, When when)
    {
        return await _db.StringSetAsync(key, value, expiry, when);
    }

    /// <summary>
    /// 保存多个 Key-value
    /// </summary>
    /// <param name="keyValuePairs"></param>
    /// <returns></returns>
    public bool StringSet(Dictionary<RedisKey, RedisValue> keyValuePairs)
    {
        var set = keyValuePairs.Select(x => x).ToArray();
        return _db.StringSet(set);
    }

    /// <summary>
    /// 保存一组字符串值
    /// </summary>
    /// <param name="keyValuePairs"></param>
    /// <returns></returns>
    public async Task<bool> StringSetAsync(Dictionary<RedisKey, RedisValue> keyValuePairs)
    {
        var set = keyValuePairs.Select(x => x).ToArray();
        return await _db.StringSetAsync(set.ToArray());
    }

    /// <summary>
    /// 获取字符串
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string StringGet(RedisKey key)
    {
        return _db.StringGet(key);
    }


    /// <summary>
    /// 获取单个值
    /// </param>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<string> StringGetAsync(RedisKey key)
    {
        return await _db.StringGetAsync(key);
    }

    /// <summary>
    /// 存储一个对象
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public bool StringSet<T>(RedisKey key, T value, TimeSpan? expiry = null)
    {
        var json = JsonConvert.SerializeObject(value);
        return _db.StringSet(key, json, expiry);
    }

    /// <summary>
    /// 存储一个对象
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<bool> StringSetAsync<T>(RedisKey key, T value, TimeSpan? expiry = null)
    {
        var json = JsonConvert.SerializeObject(value);
        return await _db.StringSetAsync(key, json, expiry);
    }

    /// <summary>
    /// 获取一个对象
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T StringGet<T>(RedisKey key)
    {
        var value = _db.StringGet(key);
        return JsonConvert.DeserializeObject<T>(value);
    }

    /// <summary>
    /// 获取一个对象
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<T> StringGetAsync<T>(RedisKey key)
    {
        var json = await _db.StringGetAsync(key);
        return JsonConvert.DeserializeObject<T>(json);
    }

    #endregion String 操作

    #region Hash 操作

    /// <summary>
    /// 判断该字段是否存在 hash 中
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public bool HashExists(RedisKey key, string hashField)
    {
        return _db.HashExists(key, hashField);
    }

    /// <summary>
    /// 判断该字段是否存在 hash 中
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public async Task<bool> HashExistsAsync(RedisKey key, string hashField)
    {
        return await _db.HashExistsAsync(key, hashField);
    }

    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public bool HashDelete(RedisKey key, string hashField)
    {
        return _db.HashDelete(key, hashField);
    }


    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public async Task<bool> HashDeleteAsync(RedisKey key, string hashField)
    {
        return await _db.HashDeleteAsync(key, hashField);
    }

    /// <summary>
    /// 从 hash 中移除指定字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashFields"></param>
    /// <returns></returns>
    public long HashDelete(RedisKey key, IEnumerable<string> hashFields)
    {
        return _db.HashDelete(key, hashFields.Select(x => new RedisValue(x)).ToArray());
    }

    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashFields"></param>
    /// <returns></returns>
    public async Task<long> HashDeleteAsync(RedisKey key, IEnumerable<string> hashFields)
    {
        return await _db.HashDeleteAsync(key, hashFields.Select(x => new RedisValue(x)).ToArray());
    }

    /// <summary>
    /// 在 hash 设定值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool HashSet(RedisKey key, string hashField, string value)
    {
        return _db.HashSet(key, hashField, value);
    }

    /// <summary>
    /// 在 hash 中设定值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashFields"></param>
    public void HashSet(RedisKey key, IEnumerable<HashEntry> hashFields)
    {
        _db.HashSet(key, hashFields.ToArray());
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public RedisValue HashGet(RedisKey key, string hashField)
    {
        return _db.HashGet(key, hashField);
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public RedisValue[] HashGet(RedisKey key, RedisValue[] hashField)
    {
        return _db.HashGet(key, hashField);
    }

    /// <summary>
    /// 从 hash 返回所有的字段值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<RedisValue> HashKeys(RedisKey key)
    {
        return _db.HashKeys(key);
    }

    /// <summary>
    /// 返回 hash 中的所有值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public RedisValue[] HashValues(RedisKey key)
    {
        return _db.HashValues(key);
    }

    /// <summary>
    /// 在 hash 设定值（序列化）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool HashSet<T>(RedisKey key, string hashField, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        return _db.HashSet(key, hashField, json);
    }

    /// <summary>
    /// 在 hash 中获取值（反序列化）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public T HashGet<T>(RedisKey key, string hashField)
    {
        return JsonConvert.DeserializeObject<T>(_db.HashGet(key, hashField));
    }

    #region async

    /// <summary>
    /// 在 hash 设定值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<bool> HashSetAsync(RedisKey key, string hashField, string value)
    {
        return await _db.HashSetAsync(key, hashField, value);
    }

    /// <summary>
    /// 在 hash 中设定值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashFields"></param>
    public async Task HashSetAsync(RedisKey key, IEnumerable<HashEntry> hashFields)
    {
        await _db.HashSetAsync(key, hashFields.ToArray());
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public async Task<RedisValue> HashGetAsync(RedisKey key, string hashField)
    {
        return await _db.HashGetAsync(key, hashField);
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RedisValue>> HashGetAsync(RedisKey key, RedisValue[] hashField)
    {
        return await _db.HashGetAsync(key, hashField);
    }

    /// <summary>
    /// 从 hash 返回所有的字段值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RedisValue>> HashKeysAsync(RedisKey key)
    {
        return await _db.HashKeysAsync(key);
    }

    /// <summary>
    /// 返回 hash 中的所有值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RedisValue>> HashValuesAsync(RedisKey key)
    {
        return await _db.HashValuesAsync(key);
    }

    /// <summary>
    /// 在 hash 设定值（序列化）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<bool> HashSetAsync<T>(RedisKey key, string hashField, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        return await _db.HashSetAsync(key, hashField, json);
    }

    /// <summary>
    /// 在 hash 中获取值（反序列化）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    public async Task<T> HashGetAsync<T>(RedisKey key, string hashField)
    {
        return JsonConvert.DeserializeObject<T>(await _db.HashGetAsync(key, hashField));
    }

    /// <summary>
    /// 获取hash中的所有字段值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> HashGetAllAsync(RedisKey key)
    {
        var entries = await _db.HashGetAllAsync(key);
        var dict = new Dictionary<string, string>(entries?.Length ?? 0);
        if (entries == null || entries.Length == 0)
            return dict;

        foreach (var e in entries)
        {
            // RedisValue -> string
            dict[e.Name.ToString()] = e.Value.ToString();
        }

        return dict;
    }

    #endregion async

    #endregion Hash 操作

    #region List 操作

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string ListLeftPop(RedisKey key)
    {
        return _db.ListLeftPop(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string ListRightPop(RedisKey key)
    {
        return _db.ListRightPop(key);
    }

    /// <summary>
    /// 移除列表指定键上与该值相同的元素
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long ListRemove(RedisKey key, string value)
    {
        return _db.ListRemove(key, value);
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long ListRightPush(RedisKey key, string value)
    {
        return _db.ListRightPush(key, value);
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long ListLeftPush(RedisKey key, string value)
    {
        return _db.ListLeftPush(key, value);
    }

    /// <summary>
    /// 返回列表上该键的长度，如果不存在，返回 0
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public long ListLength(RedisKey key)
    {
        return _db.ListLength(key);
    }

    /// <summary>
    /// 返回在该列表上键所对应的元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<RedisValue> ListRange(RedisKey key)
    {
        return _db.ListRange(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T ListLeftPop<T>(RedisKey key)
    {
        return JsonConvert.DeserializeObject<T>(_db.ListLeftPop(key));
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T ListRightPop<T>(RedisKey key)
    {
        return JsonConvert.DeserializeObject<T>(_db.ListRightPop(key));
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long ListRightPush<T>(RedisKey key, T value)
    {
        return _db.ListRightPush(key, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long ListLeftPush<T>(RedisKey key, T value)
    {
        return _db.ListLeftPush(key, JsonConvert.SerializeObject(value));
    }

    #region List-async

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<RedisValue> ListLeftPopAsync(RedisKey key)
    {
        return await _db.ListLeftPopAsync(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<RedisValue> ListRightPopAsync(RedisKey key)
    {
        return await _db.ListRightPopAsync(key);
    }

    /// <summary>
    /// 移除列表指定键上与该值相同的元素
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> ListRemoveAsync(RedisKey key, string value)
    {
        return await _db.ListRemoveAsync(key, value);
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> ListRightPushAsync(RedisKey key, string value)
    {
        return await _db.ListRightPushAsync(key, value);
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> ListLeftPushAsync(RedisKey key, string value)
    {
        return await _db.ListLeftPushAsync(key, value);
    }

    /// <summary>
    /// 返回列表上该键的长度，如果不存在，返回 0
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<long> ListLengthAsync(RedisKey key)
    {
        return await _db.ListLengthAsync(key);
    }

    /// <summary>
    /// 返回在该列表上键所对应的元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<IEnumerable<RedisValue>> ListRangeAsync(RedisKey key)
    {
        return await _db.ListRangeAsync(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<T> ListLeftPopAsync<T>(RedisKey key)
    {
        return JsonConvert.DeserializeObject<T>(await _db.ListLeftPopAsync(key));
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<T> ListRightPopAsync<T>(RedisKey key)
    {
        return JsonConvert.DeserializeObject<T>(await _db.ListRightPopAsync(key));
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> ListRightPushAsync<T>(RedisKey key, T value)
    {
        return await _db.ListRightPushAsync(key, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> ListLeftPushAsync<T>(RedisKey key, T value)
    {
        return await _db.ListLeftPushAsync(key, JsonConvert.SerializeObject(value));
    }

    #endregion List-async

    #endregion List 操作

    #region SortedSet 操作

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <param name="score"></param>
    /// <returns></returns>
    public bool SortedSetAdd(RedisKey key, string member, double score)
    {
        return _db.SortedSetAdd(key, member, score);
    }

    /// <summary>
    /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<RedisValue> SortedSetRangeByRank(RedisKey key)
    {
        return _db.SortedSetRangeByRank(key);
    }

    /// <summary>
    /// 返回有序集合的元素个数
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public long SortedSetLength(RedisKey key)
    {
        return _db.SortedSetLength(key);
    }

    /// <summary>
    /// 移除有序集合中的指定元素
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public bool SortedSetRemove(RedisKey key, string member)
    {
        return _db.SortedSetRemove(key, member);
    }

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <param name="score"></param>
    /// <returns></returns>
    public bool SortedSetAdd<T>(RedisKey key, T member, double score)
    {
        var json = JsonConvert.SerializeObject(member);
        return _db.SortedSetAdd(key, json, score);
    }

    #region SortedSet-Async

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <param name="score"></param>
    /// <returns></returns>
    public async Task<bool> SortedSetAddAsync(RedisKey key, string member, double score)
    {
        return await _db.SortedSetAddAsync(key, member, score);
    }

    /// <summary>
    /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key)
    {
        return await _db.SortedSetRangeByRankAsync(key);
    }

    /// <summary>
    /// 返回有序集合的元素个数
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<long> SortedSetLengthAsync(RedisKey key)
    {
        return await _db.SortedSetLengthAsync(key);
    }

    /// <summary>
    /// 移除有序集合中的指定元素
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public async Task<bool> SortedSetRemoveAsync(RedisKey key, string member)
    {
        return await _db.SortedSetRemoveAsync(key, member);
    }

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <param name="score"></param>
    /// <returns></returns>
    public async Task<bool> SortedSetAddAsync<T>(RedisKey key, T member, double score)
    {
        var json = JsonConvert.SerializeObject(member);
        return await _db.SortedSetAddAsync(key, json, score);
    }

    #endregion SortedSet-Async

    #endregion SortedSet 操作

    #region key 操作

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool KeyDelete(RedisKey key)
    {
        return _db.KeyDelete(key);
    }

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public long KeyDelete(IEnumerable<RedisKey> keys)
    {
        return _db.KeyDelete(keys.Select(x => x).ToArray());
    }

    /// <summary>
    /// 校验 Key 是否存在
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool KeyExists(RedisKey key)
    {
        return _db.KeyExists(key);
    }

    /// <summary>
    /// 重命名 Key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="redisNewKey"></param>
    /// <returns></returns>
    public bool KeyRename(RedisKey key, string redisNewKey)
    {
        return _db.KeyRename(key, redisNewKey);
    }

    /// <summary>
    /// 设置 Key 的时间
    /// </summary>
    /// <param name="key"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public bool KeyExpire(RedisKey key, TimeSpan? expiry)
    {
        return _db.KeyExpire(key, expiry);
    }

    #region key-async

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> KeyDeleteAsync(RedisKey key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task<long> KeyDeleteAsync(IEnumerable<RedisKey> keys)
    {
        return await _db.KeyDeleteAsync(keys.Select(x => x).ToArray());
    }

    /// <summary>
    /// 校验 Key 是否存在
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> KeyExistsAsync(RedisKey key)
    {
        return await _db.KeyExistsAsync(key);
    }

    /// <summary>
    /// 重命名 Key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="redisNewKey"></param>
    /// <returns></returns>
    public async Task<bool> KeyRenameAsync(RedisKey key, string redisNewKey)
    {
        return await _db.KeyRenameAsync(key, redisNewKey);
    }

    /// <summary>
    /// 设置 Key 的时间
    /// </summary>
    /// <param name="key"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }

    #endregion key-async

    #endregion key 操作

    #region 发布订阅

    /// <summary>
    /// 订阅
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="handle"></param>
    public void Subscribe(string channel, Action<RedisChannel, RedisValue> handle)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        sub.Subscribe(new RedisChannel(channel, RedisChannel.PatternMode.Auto), handle);
    }

    /// <summary>
    /// 发布
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public long Publish(string channel, RedisValue message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return sub.Publish(new RedisChannel(channel, RedisChannel.PatternMode.Auto), message);
    }

    /// <summary>
    /// 发布（使用序列化）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public long Publish<T>(string channel, T message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return sub.Publish(new RedisChannel(channel, RedisChannel.PatternMode.Auto), JsonConvert.SerializeObject(message));
    }


    /// <summary>
    /// 订阅
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="handle"></param>
    public async Task SubscribeAsync(string channel, Action<string, string> handle)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        await sub.SubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), (c, v) => { handle.Invoke(c, v); });
    }

    /// <summary>
    /// 发布
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<long> PublishAsync(string channel, string message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return await sub.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), message);
    }

    /// <summary>
    /// 发布
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<long> PublishAsync<T>(string channel, T message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return await sub.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), JsonConvert.SerializeObject(message));
    }

    #endregion 发布订阅

    #region 注册事件

    /// <summary>
    /// 添加注册事件
    /// </summary>
    private void AddRegisterEvent()
    {
        _connectionMultiplexer.ConnectionRestored += ConnMultiplexer_ConnectionRestored;
        _connectionMultiplexer.ConnectionFailed += ConnMultiplexer_ConnectionFailed;
        _connectionMultiplexer.ErrorMessage += ConnMultiplexer_ErrorMessage;
        _connectionMultiplexer.ConfigurationChanged += ConnMultiplexer_ConfigurationChanged;
        _connectionMultiplexer.HashSlotMoved += ConnMultiplexer_HashSlotMoved;
        _connectionMultiplexer.InternalError += ConnMultiplexer_InternalError;
        _connectionMultiplexer.ConfigurationChangedBroadcast += ConnMultiplexer_ConfigurationChangedBroadcast;
    }

    /// <summary>
    /// 重新配置广播时（通常意味着主从同步更改）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ConnMultiplexer_ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
    {
        _logger.LogDebug("Redis 集群配置广播: {EndPoint}", e.EndPoint);
    }

    private void ConnMultiplexer_InternalError(object sender, InternalErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis 内部错误");
    }

    private void ConnMultiplexer_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
    {
        _logger.LogDebug("Redis 哈希槽迁移: {OldEndPoint} -> {NewEndPoint}", e.OldEndPoint, e.NewEndPoint);
    }

    private void ConnMultiplexer_ConfigurationChanged(object sender, EndPointEventArgs e)
    {
        _logger.LogInformation("Redis 配置变更: {EndPoint}", e.EndPoint);
    }

    private void ConnMultiplexer_ErrorMessage(object sender, RedisErrorEventArgs e)
    {
        _logger.LogError("Redis 错误: {Message}", e.Message);
    }

    private void ConnMultiplexer_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis 连接失败");
    }

    private void ConnMultiplexer_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
    {
        _logger.LogInformation("Redis 连接恢复");
    }

    #endregion 注册事件
}

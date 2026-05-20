using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 判断该字段是否存在 hash 中
    /// </summary>
    public bool HashExists(RedisKey key, string hashField)
    {
        return _db.HashExists(key, hashField);
    }

    /// <summary>
    /// 判断该字段是否存在 hash 中
    /// </summary>
    public async Task<bool> HashExistsAsync(RedisKey key, string hashField)
    {
        return await _db.HashExistsAsync(key, hashField);
    }

    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    public bool HashDelete(RedisKey key, string hashField)
    {
        return _db.HashDelete(key, hashField);
    }

    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    public async Task<bool> HashDeleteAsync(RedisKey key, string hashField)
    {
        return await _db.HashDeleteAsync(key, hashField);
    }

    /// <summary>
    /// 从 hash 中移除指定字段
    /// </summary>
    public long HashDelete(RedisKey key, IEnumerable<string> hashFields)
    {
        return _db.HashDelete(key, hashFields.Select(x => new RedisValue(x)).ToArray());
    }

    /// <summary>
    /// 从hash中移除指定字段
    /// </summary>
    public async Task<long> HashDeleteAsync(RedisKey key, IEnumerable<string> hashFields)
    {
        return await _db.HashDeleteAsync(key, hashFields.Select(x => new RedisValue(x)).ToArray());
    }

    /// <summary>
    /// 在 hash 设定值
    /// </summary>
    public bool HashSet(RedisKey key, string hashField, string value)
    {
        return _db.HashSet(key, hashField, value);
    }

    /// <summary>
    /// 在 hash 中设定值
    /// </summary>
    public void HashSet(RedisKey key, IEnumerable<HashEntry> hashFields)
    {
        _db.HashSet(key, hashFields.ToArray());
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    public RedisValue HashGet(RedisKey key, string hashField)
    {
        return _db.HashGet(key, hashField);
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    public RedisValue[] HashGet(RedisKey key, RedisValue[] hashField)
    {
        return _db.HashGet(key, hashField);
    }

    /// <summary>
    /// 从 hash 返回所有的字段值
    /// </summary>
    public IEnumerable<RedisValue> HashKeys(RedisKey key)
    {
        return _db.HashKeys(key);
    }

    /// <summary>
    /// 返回 hash 中的所有值
    /// </summary>
    public RedisValue[] HashValues(RedisKey key)
    {
        return _db.HashValues(key);
    }

    /// <summary>
    /// 在 hash 设定值（序列化）
    /// </summary>
    public bool HashSet<T>(RedisKey key, string hashField, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        return _db.HashSet(key, hashField, json);
    }

    /// <summary>
    /// 在 hash 中获取值（反序列化）
    /// </summary>
    public T HashGet<T>(RedisKey key, string hashField)
    {
        return DeserializeRedisValue<T>(_db.HashGet(key, hashField));
    }

    /// <summary>
    /// 在 hash 设定值
    /// </summary>
    public async Task<bool> HashSetAsync(RedisKey key, string hashField, string value)
    {
        return await _db.HashSetAsync(key, hashField, value);
    }

    /// <summary>
    /// 在 hash 中设定值
    /// </summary>
    public async Task HashSetAsync(RedisKey key, IEnumerable<HashEntry> hashFields)
    {
        await _db.HashSetAsync(key, hashFields.ToArray());
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    public async Task<RedisValue> HashGetAsync(RedisKey key, string hashField)
    {
        return await _db.HashGetAsync(key, hashField);
    }

    /// <summary>
    /// 在 hash 中获取值
    /// </summary>
    public async Task<IEnumerable<RedisValue>> HashGetAsync(RedisKey key, RedisValue[] hashField)
    {
        return await _db.HashGetAsync(key, hashField);
    }

    /// <summary>
    /// 从 hash 返回所有的字段值
    /// </summary>
    public async Task<IEnumerable<RedisValue>> HashKeysAsync(RedisKey key)
    {
        return await _db.HashKeysAsync(key);
    }

    /// <summary>
    /// 返回 hash 中的所有值
    /// </summary>
    public async Task<IEnumerable<RedisValue>> HashValuesAsync(RedisKey key)
    {
        return await _db.HashValuesAsync(key);
    }

    /// <summary>
    /// 在 hash 设定值（序列化）
    /// </summary>
    public async Task<bool> HashSetAsync<T>(RedisKey key, string hashField, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        return await _db.HashSetAsync(key, hashField, json);
    }

    /// <summary>
    /// 在 hash 中获取值（反序列化）
    /// </summary>
    public async Task<T> HashGetAsync<T>(RedisKey key, string hashField)
    {
        return DeserializeRedisValue<T>(await _db.HashGetAsync(key, hashField));
    }

    /// <summary>
    /// 获取hash中的所有字段值
    /// </summary>
    public async Task<Dictionary<string, string>> HashGetAllAsync(RedisKey key)
    {
        var entries = await _db.HashGetAllAsync(key);
        var dict = new Dictionary<string, string>(entries?.Length ?? 0);
        if (entries == null || entries.Length == 0)
        {
            return dict;
        }

        foreach (var e in entries)
        {
            dict[e.Name.ToString()] = e.Value.ToString();
        }

        return dict;
    }
}

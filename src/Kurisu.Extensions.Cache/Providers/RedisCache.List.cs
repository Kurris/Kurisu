using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    public string ListLeftPop(RedisKey key)
    {
        return _db.ListLeftPop(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    public string ListRightPop(RedisKey key)
    {
        return _db.ListRightPop(key);
    }

    /// <summary>
    /// 移除列表指定键上与该值相同的元素
    /// </summary>
    public long ListRemove(RedisKey key, string value)
    {
        return _db.ListRemove(key, value);
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public long ListRightPush(RedisKey key, string value)
    {
        return _db.ListRightPush(key, value);
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public long ListLeftPush(RedisKey key, string value)
    {
        return _db.ListLeftPush(key, value);
    }

    /// <summary>
    /// 返回列表上该键的长度，如果不存在，返回 0
    /// </summary>
    public long ListLength(RedisKey key)
    {
        return _db.ListLength(key);
    }

    /// <summary>
    /// 返回在该列表上键所对应的元素
    /// </summary>
    public IEnumerable<RedisValue> ListRange(RedisKey key)
    {
        return _db.ListRange(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    public T ListLeftPop<T>(RedisKey key)
    {
        return DeserializeRedisValue<T>(_db.ListLeftPop(key));
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    public T ListRightPop<T>(RedisKey key)
    {
        return DeserializeRedisValue<T>(_db.ListRightPop(key));
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public long ListRightPush<T>(RedisKey key, T value)
    {
        return _db.ListRightPush(key, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public long ListLeftPush<T>(RedisKey key, T value)
    {
        return _db.ListLeftPush(key, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    public async Task<RedisValue> ListLeftPopAsync(RedisKey key)
    {
        return await _db.ListLeftPopAsync(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    public async Task<RedisValue> ListRightPopAsync(RedisKey key)
    {
        return await _db.ListRightPopAsync(key);
    }

    /// <summary>
    /// 移除列表指定键上与该值相同的元素
    /// </summary>
    public async Task<long> ListRemoveAsync(RedisKey key, string value)
    {
        return await _db.ListRemoveAsync(key, value);
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public async Task<long> ListRightPushAsync(RedisKey key, string value)
    {
        return await _db.ListRightPushAsync(key, value);
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public async Task<long> ListLeftPushAsync(RedisKey key, string value)
    {
        return await _db.ListLeftPushAsync(key, value);
    }

    /// <summary>
    /// 返回列表上该键的长度，如果不存在，返回 0
    /// </summary>
    public async Task<long> ListLengthAsync(RedisKey key)
    {
        return await _db.ListLengthAsync(key);
    }

    /// <summary>
    /// 返回在该列表上键所对应的元素
    /// </summary>
    public async Task<IEnumerable<RedisValue>> ListRangeAsync(RedisKey key)
    {
        return await _db.ListRangeAsync(key);
    }

    /// <summary>
    /// 移除并返回存储在该键列表的第一个元素
    /// </summary>
    public async Task<T> ListLeftPopAsync<T>(RedisKey key)
    {
        return DeserializeRedisValue<T>(await _db.ListLeftPopAsync(key));
    }

    /// <summary>
    /// 移除并返回存储在该键列表的最后一个元素
    /// </summary>
    public async Task<T> ListRightPopAsync<T>(RedisKey key)
    {
        return DeserializeRedisValue<T>(await _db.ListRightPopAsync(key));
    }

    /// <summary>
    /// 在列表尾部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public async Task<long> ListRightPushAsync<T>(RedisKey key, T value)
    {
        return await _db.ListRightPushAsync(key, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在列表头部插入值。如果键不存在，先创建再插入值
    /// </summary>
    public async Task<long> ListLeftPushAsync<T>(RedisKey key, T value)
    {
        return await _db.ListLeftPushAsync(key, JsonConvert.SerializeObject(value));
    }
}

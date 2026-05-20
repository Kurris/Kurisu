using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 设置key并保存字符串(如果key已存在,则覆盖值)
    /// </summary>
    public bool StringSet(RedisKey key, string value, TimeSpan? expiry = null)
    {
        return _db.StringSet(key, value, expiry);
    }

    /// <summary>
    /// 设置key并保存字符串，可通过 when 控制覆盖策略。
    /// </summary>
    public bool StringSet(RedisKey key, string value, TimeSpan? expiry, When when)
    {
        return _db.StringSet(key, value, expiry, when);
    }

    /// <summary>
    /// 保存一个字符串值
    /// </summary>
    public async Task<bool> StringSetAsync(RedisKey key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry);
    }

    /// <summary>
    /// 异步保存一个字符串值，可通过 when 控制覆盖策略。
    /// </summary>
    public async Task<bool> StringSetAsync(RedisKey key, string value, TimeSpan? expiry, When when)
    {
        return await _db.StringSetAsync(key, value, expiry, when);
    }

    /// <summary>
    /// 保存多个 Key-value
    /// </summary>
    public bool StringSet(Dictionary<RedisKey, RedisValue> keyValuePairs)
    {
        var set = keyValuePairs.Select(x => x).ToArray();
        return _db.StringSet(set);
    }

    /// <summary>
    /// 保存一组字符串值
    /// </summary>
    public async Task<bool> StringSetAsync(Dictionary<RedisKey, RedisValue> keyValuePairs)
    {
        var set = keyValuePairs.Select(x => x).ToArray();
        return await _db.StringSetAsync(set.ToArray());
    }

    /// <summary>
    /// 获取字符串
    /// </summary>
    public string StringGet(RedisKey key)
    {
        return _db.StringGet(key);
    }

    /// <summary>
    /// 获取单个值
    /// </summary>
    public async Task<string> StringGetAsync(RedisKey key)
    {
        return await _db.StringGetAsync(key);
    }

}

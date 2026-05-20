using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 移除指定 Key
    /// </summary>
    public bool KeyDelete(RedisKey key)
    {
        return _db.KeyDelete(key);
    }

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    public long KeyDelete(IEnumerable<RedisKey> keys)
    {
        return _db.KeyDelete(keys.Select(x => x).ToArray());
    }

    /// <summary>
    /// 校验 Key 是否存在
    /// </summary>
    public bool KeyExists(RedisKey key)
    {
        return _db.KeyExists(key);
    }

    /// <summary>
    /// 重命名 Key
    /// </summary>
    public bool KeyRename(RedisKey key, string redisNewKey)
    {
        return _db.KeyRename(key, redisNewKey);
    }

    /// <summary>
    /// 设置 Key 的时间
    /// </summary>
    public bool KeyExpire(RedisKey key, TimeSpan? expiry)
    {
        return _db.KeyExpire(key, expiry);
    }

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    public async Task<bool> KeyDeleteAsync(RedisKey key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 移除指定 Key
    /// </summary>
    public async Task<long> KeyDeleteAsync(IEnumerable<RedisKey> keys)
    {
        return await _db.KeyDeleteAsync(keys.Select(x => x).ToArray());
    }

    /// <summary>
    /// 校验 Key 是否存在
    /// </summary>
    public async Task<bool> KeyExistsAsync(RedisKey key)
    {
        return await _db.KeyExistsAsync(key);
    }

    /// <summary>
    /// 重命名 Key
    /// </summary>
    public async Task<bool> KeyRenameAsync(RedisKey key, string redisNewKey)
    {
        return await _db.KeyRenameAsync(key, redisNewKey);
    }

    /// <summary>
    /// 设置 Key 的时间
    /// </summary>
    public async Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }
}

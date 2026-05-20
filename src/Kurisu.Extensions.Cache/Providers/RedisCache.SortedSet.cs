using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// SortedSet 新增
    /// </summary>
    public bool SortedSetAdd(RedisKey key, string member, double score)
    {
        return _db.SortedSetAdd(key, member, score);
    }

    /// <summary>
    /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
    /// </summary>
    public IEnumerable<RedisValue> SortedSetRangeByRank(RedisKey key)
    {
        return _db.SortedSetRangeByRank(key);
    }

    /// <summary>
    /// 返回有序集合的元素个数
    /// </summary>
    public long SortedSetLength(RedisKey key)
    {
        return _db.SortedSetLength(key);
    }

    /// <summary>
    /// 移除有序集合中的指定元素
    /// </summary>
    public bool SortedSetRemove(RedisKey key, string member)
    {
        return _db.SortedSetRemove(key, member);
    }

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    public bool SortedSetAdd<T>(RedisKey key, T member, double score)
    {
        var json = JsonConvert.SerializeObject(member);
        return _db.SortedSetAdd(key, json, score);
    }

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    public async Task<bool> SortedSetAddAsync(RedisKey key, string member, double score)
    {
        return await _db.SortedSetAddAsync(key, member, score);
    }

    /// <summary>
    /// 在有序集合中返回指定范围的元素，默认情况下从低到高。
    /// </summary>
    public async Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key)
    {
        return await _db.SortedSetRangeByRankAsync(key);
    }

    /// <summary>
    /// 返回有序集合的元素个数
    /// </summary>
    public async Task<long> SortedSetLengthAsync(RedisKey key)
    {
        return await _db.SortedSetLengthAsync(key);
    }

    /// <summary>
    /// 移除有序集合中的指定元素
    /// </summary>
    public async Task<bool> SortedSetRemoveAsync(RedisKey key, string member)
    {
        return await _db.SortedSetRemoveAsync(key, member);
    }

    /// <summary>
    /// SortedSet 新增
    /// </summary>
    public async Task<bool> SortedSetAddAsync<T>(RedisKey key, T member, double score)
    {
        var json = JsonConvert.SerializeObject(member);
        return await _db.SortedSetAddAsync(key, json, score);
    }
}

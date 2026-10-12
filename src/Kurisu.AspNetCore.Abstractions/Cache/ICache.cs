namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 缓存接口
/// </summary>
public interface ICache
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存的Key</param>
    /// <returns>返回缓存的值</returns>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存的Key</param>
    /// <param name="value">缓存的值</param>
    /// <param name="expiry">缓存的过期时间</param>
    /// <returns>返回操作是否成功</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// 移除缓存值
    /// </summary>
    /// <param name="key">缓存的Key</param>
    /// <returns>返回操作是否成功</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>批量删除缓存，返回实际删除条目数；不存在的 Key 不算失败。不同分组可能部分成功，不保证跨 Key 原子性。</summary>
    /// <param name="keys">待删除 Key；不能为空引用，条目不能为空白；重复项去重，空集合返回零。</param>
    /// <returns>实际删除条目数。</returns>
    async Task<long> RemoveManyAsync(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var distinctKeys = keys.Distinct(StringComparer.Ordinal).ToArray();
        foreach (var key in distinctKeys) ArgumentException.ThrowIfNullOrWhiteSpace(key);
        // 为既有提供者保留兼容实现；支持原生批量删除的提供者应覆盖此方法。
        var results = await Task.WhenAll(distinctKeys.Select(async key => await RemoveAsync(key)));
        return results.LongCount(removed => removed);
    }

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存的Key</param>
    /// <returns>返回缓存是否存在</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 获取缓存值，不存在时通过 factory 创建并写入缓存
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存的Key</param>
    /// <param name="factory">缓存不存在时的值创建工厂</param>
    /// <param name="expiry">缓存的过期时间</param>
    /// <returns>返回缓存的值</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
}
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

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存的Key</param>
    /// <returns>返回缓存是否存在</returns>
    Task<bool> ExistsAsync(string key);
}

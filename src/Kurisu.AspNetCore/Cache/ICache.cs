using System;
using System.Threading.Tasks;

namespace Kurisu.AspNetCore.Cache;

/// <summary>
/// 缓存
/// </summary>
public interface ICache
{
    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Get<T>(string key);
    
    /// <summary>
    /// 设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Set<T>(string key, T value);
    
    /// <summary>
    /// 设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Set<T>(string key, T value, TimeSpan expiration);
    
    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> GetAsync<T>(string key);
    
    /// <summary>
    /// 设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> SetAsync<T>(string key, T value);
    
    /// <summary>
    /// 设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> SetAsync<T>(string key, T value, TimeSpan expiration);
}
using System;
using System.Threading.Tasks;

namespace Kurisu.AspNetCore.Cache;

/// <summary>
/// 通用缓存
/// </summary>
internal class CommonCache : ICache
{
    /// <summary>
    /// ctor
    /// </summary>
    public CommonCache()
    {
    }

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public T Set<T>(string key, T value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public T Set<T>(string key, T value, TimeSpan expiration)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<T> SetAsync<T>(string key, T value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<T> SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        throw new NotImplementedException();
    }
}
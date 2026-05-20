using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Kurisu.Extensions.Cache.Providers;

public partial class RedisCache
{
    /// <summary>
    /// 订阅
    /// </summary>
    public void Subscribe(string channel, Action<RedisChannel, RedisValue> handle)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        sub.Subscribe(new RedisChannel(channel, RedisChannel.PatternMode.Auto), handle);
    }

    /// <summary>
    /// 发布
    /// </summary>
    public long Publish(string channel, RedisValue message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return sub.Publish(new RedisChannel(channel, RedisChannel.PatternMode.Auto), message);
    }

    /// <summary>
    /// 发布（使用序列化）
    /// </summary>
    public long Publish<T>(string channel, T message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return sub.Publish(new RedisChannel(channel, RedisChannel.PatternMode.Auto), JsonConvert.SerializeObject(message));
    }

    /// <summary>
    /// 订阅
    /// </summary>
    public async Task SubscribeAsync(string channel, Action<string, string> handle)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        await sub.SubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), (c, v) => { handle.Invoke(c, v); });
    }

    /// <summary>
    /// 发布
    /// </summary>
    public async Task<long> PublishAsync(string channel, string message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return await sub.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), message);
    }

    /// <summary>
    /// 发布
    /// </summary>
    public async Task<long> PublishAsync<T>(string channel, T message)
    {
        var sub = _connectionMultiplexer.GetSubscriber();
        return await sub.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Auto), JsonConvert.SerializeObject(message));
    }
}

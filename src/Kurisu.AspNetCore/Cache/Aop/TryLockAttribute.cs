using System;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Cache.Aop;

/// <summary>
/// 操作锁定
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TryLockAttribute : AopAttribute
{
    /// <summary>
    /// 场景
    /// </summary>
    private readonly string _sence;
    private readonly string _tips;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="sence">场景</param>
    /// <param name="tips"></param>
    public TryLockAttribute(string sence, string tips)
    {
        _sence = sence;
        _tips = tips;
    }

    /// <summary>
    /// 过期时间 
    /// </summary>
    public TimeSpan? Expiry { get; set; }

    /// <summary>
    /// 重试间隔 
    /// </summary>
    public TimeSpan? RetryInterval { get; set; }

    /// <summary>
    /// 重试次数 
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// invoke
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var value = context.Parameters[0];
        string redisKey = value switch
        {
            string s => s,
            ICacheKey k => k.GetCacheKey(context.ServiceProvider),
            _ => throw new ArgumentException("First parameter must be string or implement ICacheKey.")
        };

        var cacheProvider = context.ServiceProvider.GetRequiredService<RedisCache>();
        await using var locker = await cacheProvider.LockAsync(GetLockKey(redisKey), Expiry, RetryInterval, RetryCount);
        locker.Acquired.ThrowIfFalse(_tips);
        await next(context);
    }

    private string GetLockKey(string key)
    {
        return $"{_sence}:{key}";
    }
}
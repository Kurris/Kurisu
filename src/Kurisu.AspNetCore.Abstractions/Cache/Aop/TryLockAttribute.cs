using AspectCore.DynamicProxy;
using AspectCore.DynamicProxy.Parameters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Cache.Aop;

/// <summary>
/// 操作锁定
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TryLockAttribute(string scene, string tips) : AopAttribute
{
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
    /// 锁Key参数索引（默认从首个参数获取）
    /// </summary>
    public int KeyParameterIndex { get; set; } = 0;

    /// <summary>
    /// invoke
    /// </summary>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var keyParam = context.GetParameters(true)[KeyParameterIndex];
        var lockable = context.ServiceProvider.GetRequiredService<ILockable>();
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = GetTimeModeHandler(),
            RetryStrategy = GetRetryStrategy()
        };

        await using var multiLock = await MultiLock.AcquireAsync(lockable,
        scene,
        context.Parameters[KeyParameterIndex],
        keyParam.Name, KeyParameterIndex,
        options, tips);
        await next(context);
    }

    /// <summary>
    /// 获取锁时间模式处理器
    /// </summary>
    protected virtual IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.InfiniteRenewal(Expiry);
    }

    /// <summary>
    /// 获取重试策略
    /// </summary>
    protected virtual IDistributedLockRetryStrategy GetRetryStrategy()
    {
        return new DefaultLockRetryStrategy(RetryInterval, RetryCount);
    }
}

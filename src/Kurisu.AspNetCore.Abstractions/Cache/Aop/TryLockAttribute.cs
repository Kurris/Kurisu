using AspectCore.DynamicProxy;
using AspectCore.DynamicProxy.Parameters;
using Kurisu.AspNetCore.Abstractions.Result;
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
        var keys = ResolveLockKeys(context.ServiceProvider, keyParam.Name, context.Parameters[KeyParameterIndex]);
        var lockable = context.ServiceProvider.GetRequiredService<ILockable>();
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = GetTimeModeHandler(),
            RetryStrategy = GetRetryStrategy()
        };

        var acquiredLocks = new List<ILockHandler>(keys.Length);

        try
        {
            foreach (var key in keys)
            {
                var locker = await lockable.LockAsync(GetLockKey(key), options);
                acquiredLocks.Add(locker);
                locker.Acquired.ThrowIfFalse(tips);
            }

            await next(context);
        }
        finally
        {
            for (var i = acquiredLocks.Count - 1; i >= 0; i--)
            {
                await acquiredLocks[i].DisposeAsync();
            }
        }
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

    private string GetLockKey(string key)
    {
        return $"Locker:{scene}:{key}";
    }

    private string[] ResolveLockKeys(IServiceProvider serviceProvider, string parameter, object value)
    {
        IEnumerable<string> keys = value switch
        {
            string s => [s],
            ITryLockKey k => [k.GetKey(serviceProvider)],
            ITryLockKeys k => k.GetKeys(serviceProvider),
            _ => throw new ArgumentException($"方法第{KeyParameterIndex}个参数必须为string类型,或者实现{nameof(ITryLockKey)}/{nameof(ITryLockKeys)}接口.")
        };

        var lockKeys = keys?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? Array.Empty<string>();
        if (lockKeys.Length == 0)
        {
            throw new ArgumentException("必须提供至少一个有效的锁定Key.", parameter);
        }

        return lockKeys;
    }
}

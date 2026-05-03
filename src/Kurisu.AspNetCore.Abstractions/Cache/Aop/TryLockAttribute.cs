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
        var parameters = context.GetParameters(true);
        var keyParam = parameters[KeyParameterIndex];
        var lockable = context.ServiceProvider.GetRequiredService<ILockable>();
        var cancellationToken = ResolveCancellationToken(parameters);
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = GetTimeModeHandler(),
            RetryStrategy = GetRetryStrategy()
        };

        await using var multiLock = await MultiLock.AcquireAsync(lockable,
        scene,
        keyParam.Value,
        keyParam.Name, KeyParameterIndex,
        options, tips, cancellationToken);
        await next(context);
    }

    private static CancellationToken ResolveCancellationToken(ParameterCollection parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.Type == typeof(CancellationToken) && parameter.Value is CancellationToken cancellationToken)
            {
                return cancellationToken;
            }
        }

        return CancellationToken.None;
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
        return new DefaultLockRetryStrategy(RetryCount);
    }
}

using AspectCore.DynamicProxy;
using System.Collections;
using System.Globalization;
using Kurisu.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;

/// <summary>
/// 操作锁定
/// 表达式统一使用服务方法的参数名；接口代理使用接口参数名，与特性声明位置无关。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TryLockAttribute(string scene, string tips) : AopAttribute
{
    /// <summary>
    /// 锁过期时间，单位秒。
    /// </summary>
    public int? ExpirySeconds { get; protected set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 单个锁 Key 表达式，例如 input.Id；与 Keys 必须且只能指定一个。
    /// </summary>
    public string Key { get; set; }

    /// <summary>多个锁 Key 的集合表达式，例如 inputs.Select(x =&gt; x.Id)。</summary>
    public string Keys { get; set; }

    /// <summary>
    /// invoke
    /// </summary>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (Key == null == (Keys == null)) throw new ArgumentException("TryLock 必须且只能指定 Key 或 Keys。");
        var lockable = context.ServiceProvider.GetRequiredService<ILockable>();
        var expressions = context.ServiceProvider.GetRequiredService<MethodExpressionEvaluator>();
        var cancellationToken = context.Parameters.OfType<CancellationToken>().FirstOrDefault();

        var value = await expressions.EvaluateAsync<object>(context.ServiceMethod, Key ?? Keys, context.Parameters, cancellationToken);
        var values = Keys == null ? [value]
            : value is IEnumerable sequence and not string ? sequence.Cast<object>()
            : throw new ArgumentException("TryLock.Keys 必须返回集合。");

        var lockKeys = values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)).ToArray();
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = GetTimeModeHandler(),
            RetryStrategy = GetRetryStrategy()
        };

        await using var multiLock = await MultiLock.AcquireAsync(lockable, scene, lockKeys,
            options, tips, cancellationToken);
        await next(context);
    }

    /// <summary>
    /// 获取锁时间模式处理器
    /// </summary>
    protected virtual IDistributedLockTimeModeHandler GetTimeModeHandler()
    {
        return LockTimeModeHandler.InfiniteRenewal(GetExpiry());
    }

    /// <summary>
    /// 获取重试策略
    /// </summary>
    protected virtual IDistributedLockRetryStrategy GetRetryStrategy()
    {
        return new DefaultLockRetryStrategy(RetryCount);
    }

    /// <summary>
    /// 获取锁过期时间。
    /// </summary>
    protected TimeSpan? GetExpiry()
    {
        return ExpirySeconds.HasValue ? TimeSpan.FromSeconds(ExpirySeconds.Value) : null;
    }
}

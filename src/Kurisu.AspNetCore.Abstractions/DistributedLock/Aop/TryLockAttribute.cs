using Kurisu.AspNetCore.Abstractions.DistributedLock.Impl;
using Kurisu.AspNetCore.Abstractions.Aop;
using AspectCore.DynamicProxy;
using System.Collections;
using System.Globalization;
using Kurisu.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;

/// <summary>
/// 操作锁定
/// 未指定 Key / Keys 时取首参数，支持 string、long、int 及兼容接口 ITryLockKey / ITryLockKeys。
/// 表达式统一使用服务方法的参数名；接口代理使用接口参数名，与特性声明位置无关。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TryLockAttribute(string scene, string tips) : AopAttribute
{
    private int? _retryCount;
    private int? _expirySeconds;
    private int? _maxRenewalCount;

    /// <summary>
    /// 锁过期时间，单位秒；显式设置时必须大于 0，未设置时使用 DI 工厂默认值。
    /// </summary>
    public int ExpirySeconds
    {
        get => _expirySeconds ?? 6;
        set => _expirySeconds = value;
    }

    /// <summary>
    /// 最大续期次数：正数表示最多续期指定次数；
    /// <see cref="LockRenewal.Disabled"/>（0）表示不续期，
    /// <see cref="LockRenewal.Unlimited"/>（-1）表示无限续期。
    /// 未设置时使用 DI 工厂默认值。
    /// </summary>
    public int MaxRenewalCount
    {
        get => _maxRenewalCount ?? LockRenewal.Unlimited;
        set => _maxRenewalCount = value;
    }

    /// <summary>
    /// 获取锁的尝试次数上限；未显式设置时使用 DI 工厂的默认策略。
    /// </summary>
    public int RetryCount
    {
        get => _retryCount ?? 3;
        set => _retryCount = value;
    }

    /// <summary>
    /// 单个锁 Key 表达式，例如 input.Id；与 Keys 均未指定时取首参数，不能同时指定。
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 多个锁 Key 的集合表达式，例如 inputs.Select(x =&gt; x.Id)，不能与 Key 同时指定。
    /// </summary>
    public string Keys { get; set; }

    /// <summary>
    /// invoke
    /// </summary>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (Key != null && Keys != null) throw new ArgumentException("TryLock 不能同时指定 Key 和 Keys。");
        var cancellationToken = context.Parameters.OfType<CancellationToken>().FirstOrDefault();
        cancellationToken.ThrowIfCancellationRequested();

        var lockable = context.ServiceProvider.GetRequiredService<ILockable>();
        IEnumerable<string> lockKeys;
        if (Key == null && Keys == null)
        {
            lockKeys = ResolveFirstParameterKeys(context);
        }
        else
        {
            var expressions = context.ServiceProvider.GetRequiredService<MethodExpressionEvaluator>();
            var value = await expressions.EvaluateAsync<object>(context.ServiceMethod, Key ?? Keys, context.Parameters, cancellationToken);
            var values = Keys == null ? [value]
                : value is IEnumerable sequence and not string ? sequence.Cast<object>()
                : throw new ArgumentException("TryLock.Keys 必须返回集合。");
            lockKeys = values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)).ToArray();
        }

        var options = context.ServiceProvider.GetRequiredService<IDistributedLockOptionsFactory>().CreateDefault();
        options.TimeModeHandler = GetTimeModeHandler(options.TimeModeHandler);
        options.RetryStrategy = GetRetryStrategy(options.RetryStrategy);

        await using var multiLock = await MultiLock.AcquireAsync(lockable, scene, lockKeys, options, tips, cancellationToken);
        await next(context);
    }

    private static IEnumerable<string> ResolveFirstParameterKeys(AspectContext context)
    {
        if (context.Parameters.Length == 0)
            throw new ArgumentException("TryLock 未指定 Key / Keys 时必须提供首参数。");

        // 兼容旧 DTO 接口；保留两版本迁移窗口。
#pragma warning disable CS0618
        return context.Parameters[0] switch
        {
            string key => [key],
            int or long => [Convert.ToString(context.Parameters[0], CultureInfo.InvariantCulture)],
            ITryLockKey key => [key.GetKey()],
            ITryLockKeys keys => keys.GetKeys(),
            _ => throw new ArgumentException("TryLock 首参数必须为 string、long、int 或实现 ITryLockKey / ITryLockKeys；其他类型请显式指定 Key / Keys。")
        };
#pragma warning restore CS0618
    }

    /// <summary>
    /// 将显式时间配置覆盖到 DI 工厂默认配置。
    /// </summary>
    protected virtual IDistributedLockTimeModeHandler GetTimeModeHandler(IDistributedLockTimeModeHandler defaultHandler)
    {
        if (!_expirySeconds.HasValue && !_maxRenewalCount.HasValue)
            return defaultHandler;

        if (_maxRenewalCount is < LockRenewal.Unlimited)
            throw new ArgumentException("TryLock.MaxRenewalCount 必须大于或等于 -1。");

        var defaults = defaultHandler.Resolve();
        var enableAutoRenewal = _maxRenewalCount.HasValue ? _maxRenewalCount != LockRenewal.Disabled : defaults.EnableAutoRenewal;
        var maxRenewalCount = _maxRenewalCount.HasValue
            ? _maxRenewalCount > LockRenewal.Disabled ? _maxRenewalCount : null
            : defaults.MaxRenewalCount;

        return new LockTimeModeHandler(
            _expirySeconds.HasValue ? TimeSpan.FromSeconds(_expirySeconds.Value) : defaults.Expiry,
            enableAutoRenewal,
            enableAutoRenewal ? maxRenewalCount : null);
    }

    /// <summary>
    /// 将显式重试配置覆盖到 DI 工厂默认策略；未设置时返回默认策略。
    /// </summary>
    protected virtual IDistributedLockRetryStrategy GetRetryStrategy(IDistributedLockRetryStrategy defaultStrategy)
    {
        return _retryCount.HasValue ? new DefaultLockRetryStrategy(RetryCount) : defaultStrategy;
    }
}
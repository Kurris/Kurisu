using System.Collections;
using Kurisu.Expressions;
using System.Collections.Concurrent;
using System.Reflection;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Page;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.Cache.MethodCaching;

[NonAspect]
internal sealed class MethodCacheExecutor(
    ICache cache,
    ILockable lockable,
    ICacheKeyGenerator keys,
    MethodExpressionEvaluator expressions,
    MethodCacheCoordinator coordinator,
    IOptions<MethodCacheOptions> options,
    ILogger<MethodCacheExecutor> logger) : IMethodCacheExecutor
{
    // 有效期最多向下浮动 10%，分散缓存集中到期。
    private const double ExpiryJitterRatio = 0.1;

    // 空结果使用固定的短有效期，避免不存在的数据被反复查询。
    private static readonly TimeSpan NullExpiry = TimeSpan.FromSeconds(30);

    private delegate Task Invocation(MethodCacheExecutor executor, AspectContext context, AspectDelegate next, CacheableAttribute attribute);

    private static readonly ConcurrentDictionary<Type, Invocation> Invocations = new();

    public Task InvokeAsync(AspectContext context, AspectDelegate next, CacheableAttribute attribute)
    {
        var returnType = context.ServiceMethod.ReturnType;
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
            throw new NotSupportedException("Cacheable 仅支持返回 Task<T> 的查询方法。");

        var resultType = returnType.GenericTypeArguments[0];

        for (var type = resultType; type != null; type = type.BaseType)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pagination<>))
                throw new NotSupportedException("Cacheable 不支持 Pagination<T> 及其派生类型的分页返回值。");

        return Invocations.GetOrAdd(resultType, type => typeof(MethodCacheExecutor)
            .GetMethod(nameof(InvokeTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type).CreateDelegate<Invocation>())(this, context, next, attribute);
    }

    private static Task InvokeTypedAsync<T>(MethodCacheExecutor executor, AspectContext context, AspectDelegate next, CacheableAttribute attribute)
    {
        return executor.ReadThroughAsync<T>(context, next, attribute);
    }

    private async Task ReadThroughAsync<T>(AspectContext context, AspectDelegate next, CacheableAttribute attribute)
    {
        var policy = GetPolicy(attribute.Policy);
        var scope = GetScope(context);
        var callerToken = context.Parameters.OfType<CancellationToken>().FirstOrDefault();
        callerToken.ThrowIfCancellationRequested();

        if (scope.BypassCache || context.ServiceProvider.GetService<ITransactionCallbackRegistry>()?.HasActiveTransaction == true)
        {
            await next(context);
            return;
        }

        var key = attribute.Key == null
            ? keys.Generate(context, attribute.Region, policy, scope)
            : keys.Generate(attribute.Region, await expressions.EvaluateAsync<object>(context.ServiceMethod,
                attribute.Key, context.Parameters, callerToken), policy, scope);

        var first = await ReadAsync<T>(key, callerToken);
        if (ReturnHit(context, first)) return;

        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        waiting.CancelAfter(policy.LockWaitTimeout);
        using var localLease = await EnterLocalLockAsync(key, waiting.Token, callerToken);
        var (entry, handle) = await WaitForCacheOrLockAsync<T>(key, policy, waiting.Token, callerToken);
        if (ReturnHit(context, entry)) return;

        // 成功获得的锁由这里持有到查询及回填完成；查询阶段不再处理等待超时。
        try
        {
            if (ReturnHit(context, await ReadAsync<T>(key, callerToken))) return;
            await LoadAsync<T>(context, next, key, policy, callerToken, handle);
        }
        finally
        {
            await ReleaseDistributedLockAsync(handle);
        }
    }

    private async Task<IDisposable> EnterLocalLockAsync(string key, CancellationToken waitingToken, CancellationToken callerToken)
    {
        try
        {
            return await coordinator.EnterAsync(key, waitingToken);
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            throw new TimeoutException("等待进程内缓存加载锁超时。");
        }
    }

    // 本地锁由调用方持有。返回缓存封装或成功获得的锁，两者只有一个非 null。
    // 未获得的锁句柄在这里释放；成功获得的锁交给调用方释放。
    private async Task<(CacheEnvelope<T> Entry, ILockHandler Handle)> WaitForCacheOrLockAsync<T>(string key,
        MethodCachePolicy policy, CancellationToken waitingToken, CancellationToken callerToken)
    {
        try
        {
            while (true)
            {
                waitingToken.ThrowIfCancellationRequested();
                var entry = await ReadAsync<T>(key, waitingToken);
                if (entry != null) return (entry, null);

                var handle = await AcquireDistributedLockAsync(key, policy.LockExpiry, waitingToken);
                if (handle.Acquired) return (null, handle);
                await ReleaseDistributedLockAsync(handle);

                await Task.Delay(policy.LockPollInterval, waitingToken);
            }
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested && waitingToken.IsCancellationRequested)
        {
            throw new TimeoutException("等待分布式缓存加载超时。");
        }
    }

    private async Task<ILockHandler> AcquireDistributedLockAsync(string key, TimeSpan expiry, CancellationToken token)
    {
        try
        {
            return await lockable.LockAsync($"{key}:load-lock", new DistributedLockAcquisitionOptions
            {
                TimeModeHandler = LockTimeModeHandler.InfiniteRenewal(expiry),
                RetryStrategy = new DefaultLockRetryStrategy(0)
            }, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "方法缓存获取分布式锁失败，拒绝回源。");
            throw;
        }
    }

    private async Task ReleaseDistributedLockAsync(ILockHandler handle)
    {
        try
        {
            await handle.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "方法缓存分布式锁释放失败，将依赖租约过期。");
        }
    }

    private async Task LoadAsync<T>(AspectContext context, AspectDelegate next, string key,
        MethodCachePolicy policy, CancellationToken callerToken, ILockHandler lockHandle)
    {
        var result = await ExecuteQueryAsync<T>(context, next, policy.QueryTimeout, callerToken);
        if (!lockHandle.Acquired)
        {
            logger.LogWarning("方法缓存加载期间丢失分布式锁，返回结果但跳过缓存回填。");
            return;
        }

        await WriteAsync(key, result, result is null ? NullExpiry : policy.Expiry);
    }

    private static async Task<T> ExecuteQueryAsync<T>(AspectContext context, AspectDelegate next,
        TimeSpan timeout, CancellationToken callerToken)
    {
        using var query = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        query.CancelAfter(timeout);
        var parameters = context.ServiceMethod.GetParameters();
        var tokenIndices = Enumerable.Range(0, parameters.Length)
            .Where(i => parameters[i].ParameterType == typeof(CancellationToken)).ToArray();
        var originalTokens = tokenIndices.Select(i => context.Parameters[i]).ToArray();
        try
        {
            foreach (var index in tokenIndices) context.Parameters[index] = query.Token;
            // next 只执行一次。等待查询真正完成，再释放锁；不遗留使用已释放 DI 作用域的后台查询。
            await next(context);
            var result = await (Task<T>)context.ReturnValue;
            query.Token.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            for (var i = 0; i < tokenIndices.Length; i++) context.Parameters[tokenIndices[i]] = originalTokens[i];
        }
    }

    private async Task WriteAsync<T>(string key, T result, TimeSpan expiry)
    {
        expiry = TimeSpan.FromTicks(Math.Max(1, (long)(expiry.Ticks * (1 - Random.Shared.NextDouble() * ExpiryJitterRatio))));
        try
        {
            if (!await cache.SetAsync(key, new CacheEnvelope<T> { Value = result }, expiry))
            {
                logger.LogWarning("方法缓存写入返回失败，返回已完成的查询结果。");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "方法缓存写入失败，返回已完成的查询结果。");
        }
    }

    private async Task<CacheEnvelope<T>> ReadAsync<T>(string key, CancellationToken token)
    {
        try
        {
            return await cache.GetAsync<CacheEnvelope<T>>(key).WaitAsync(token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "方法缓存读取失败，拒绝回源。");
            throw;
        }
    }

    private static bool ReturnHit<T>(AspectContext context, CacheEnvelope<T> entry)
    {
        if (entry == null) return false;
        context.ReturnValue = Task.FromResult(entry.Value);
        return true;
    }

    public async Task EvictAsync(AspectContext context, AspectDelegate next, CacheEvictAttribute attribute)
    {
        if ((attribute.Key == null) == (attribute.Keys == null))
            throw new ArgumentException("CacheEvict 必须且只能指定 Key 或 Keys。");
        var token = context.Parameters.OfType<CancellationToken>().FirstOrDefault();
        var expressionMethod = context.ServiceMethod;
        if (attribute.Condition != null && !await expressions.EvaluateAsync<bool>(expressionMethod, attribute.Condition, context.Parameters, token))
        {
            await next(context);
            return;
        }

        var policy = GetPolicy(attribute.Policy);
        var scope = GetScope(context);
        var value = await expressions.EvaluateAsync<object>(expressionMethod,
            attribute.Key ?? attribute.Keys, context.Parameters, token);
        var values = attribute.Keys == null ? new[] { value }
            : value is IEnumerable sequence && value is not string ? sequence.Cast<object>()
            : throw new ArgumentException("CacheEvict.Keys 必须返回集合。");
        // 执行前物化并去重，避免 Save 回填 ID 或修改集合后删除错误的 Key。
        var cacheKeys = values.Select(v => keys.Generate(attribute.Region, v, policy, scope)).Distinct(StringComparer.Ordinal).ToArray();
        var registry = context.ServiceProvider.GetService<ITransactionCallbackRegistry>();
        await next(context);
        if (cacheKeys.Length == 0) return;

        if (registry != null) await registry.RegisterAfterCommitAsync(() => RemoveAllAsync(cacheKeys));
        else await RemoveAllAsync(cacheKeys);
    }

    private async Task RemoveAllAsync(string[] cacheKeys)
    {
        foreach (var key in cacheKeys) await RemoveAsync(key);
    }

    private async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key); // false 也可能仅表示 Key 已不存在。
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "方法缓存提交后失效失败，缓存将依赖 TTL 过期。");
            throw;
        }
    }

    private MethodCachePolicy GetPolicy(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !options.Value.Policies.TryGetValue(name, out var policy))
            throw new InvalidOperationException($"未配置方法缓存策略 '{name}'。");
        return policy;
    }

    private static MethodCacheScope GetScope(AspectContext context)
    {
        var scope = new MethodCacheScope();
        var user = context.ServiceProvider.GetService<ICurrentUser>();
        var tenant = context.ServiceProvider.GetService<ICurrentTenant>() ?? user;
        var databaseTenant = context.ServiceProvider.GetService<IDbTenantAccessor>();
        scope.Dimensions["tenant"] = databaseTenant != null ? databaseTenant.GetTenantId() : tenant?.GetTenantId();
        foreach (var contributor in context.ServiceProvider.GetServices<IMethodCacheScopeContributor>())
            contributor.Contribute(scope);
        return scope;
    }
}

/// <summary>
/// 对象存在即命中；Value 可以是 null、0 或 false。
/// </summary>
internal sealed class CacheEnvelope<T>
{
    public T Value { get; set; }
}

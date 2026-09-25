using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.Cache.MethodCaching;

[NonAspect]
internal sealed class MethodCacheExecutor(
    ICache cache, ILockable lockable, ICacheKeyGenerator keys,
    MethodCacheCoordinator coordinator, IOptions<MethodCacheOptions> options,
    ILogger<MethodCacheExecutor> logger) : IMethodCacheExecutor
{
    private delegate Task Invocation(MethodCacheExecutor executor, AspectContext context, AspectDelegate next, CacheableAttribute attribute);
    private static readonly ConcurrentDictionary<Type, Invocation> Invocations = new();
    private static readonly AsyncLocal<CallFrame> CurrentCall = new();
    private static readonly Meter Meter = new("Kurisu.MethodCache");
    private static readonly Counter<long> Events = Meter.CreateCounter<long>("kurisu.method_cache.events");

    public Task InvokeAsync(AspectContext context, AspectDelegate next, CacheableAttribute attribute)
    {
        var returnType = context.ServiceMethod.ReturnType;
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
            throw new NotSupportedException("Cacheable 第一版仅支持 Task<T> 查询方法。");
        var resultType = returnType.GenericTypeArguments[0];
        if (typeof(IQueryable).IsAssignableFrom(resultType) || typeof(Stream).IsAssignableFrom(resultType) ||
            typeof(Task).IsAssignableFrom(resultType) || resultType.IsByRefLike ||
            resultType == typeof(ValueTask) || (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>)) ||
            resultType.GetInterfaces().Append(resultType).Any(t => t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)))
            throw new NotSupportedException("Cacheable 不支持流、延迟查询或嵌套异步结果。");
        return Invocations.GetOrAdd(resultType, type => typeof(MethodCacheExecutor)
            .GetMethod(nameof(InvokeTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type).CreateDelegate<Invocation>())(this, context, next, attribute);
    }

    private static Task InvokeTypedAsync<T>(MethodCacheExecutor executor, AspectContext context,
        AspectDelegate next, CacheableAttribute attribute) => executor.ReadThroughAsync<T>(context, next, attribute);

    private async Task ReadThroughAsync<T>(AspectContext context, AspectDelegate next, CacheableAttribute attribute)
    {
        var policy = GetPolicy(attribute.Policy);
        var scope = GetScope(context, policy);
        var callerToken = context.Parameters.OfType<CancellationToken>().FirstOrDefault();
        callerToken.ThrowIfCancellationRequested();
        var key = keys.Generate(context, attribute.Region, attribute.KeyParameterIndex, policy, scope);
        if (scope.BypassCache || context.ServiceProvider.GetService<ITransactionCallbackRegistry>()?.HasActiveTransaction == true)
        {
            Record("bypass");
            await next(context);
            return;
        }

        for (var frame = CurrentCall.Value; frame != null; frame = frame.Parent)
            if (frame.Key == key) throw new InvalidOperationException("检测到同一缓存 Key 的递归查询。");
        var previous = CurrentCall.Value;
        CurrentCall.Value = new CallFrame(key, previous);
        try
        {
            var first = await ReadAsync<T>(key, policy, callerToken);
            if (ReturnHit(context, first.Entry)) return;
            Record("miss");
            using var waiting = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            waiting.CancelAfter(policy.LockWaitTimeout);
            IDisposable localLease;
            try
            {
                localLease = await coordinator.EnterAsync(key, waiting.Token);
            }
            catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
            {
                // 无本地锁时不能降级，否则本地也会发生无界并发回源。
                Record("lock_timeout");
                throw new TimeoutException("等待本地缓存加载超时。");
            }
            using (localLease)
            {
                var queryStarted = false;
                try
                {
                    while (true)
                    {
                        waiting.Token.ThrowIfCancellationRequested();
                        var read = await ReadAsync<T>(key, policy, waiting.Token);
                        if (ReturnHit(context, read.Entry)) return;
                        if (read.Failed)
                        {
                            queryStarted = true;
                            await LoadAsync<T>(context, next, key, policy, callerToken, false);
                            return;
                        }

                        ILockHandler handle;
                        try
                        {
                            handle = await lockable.LockAsync($"{key}:load-lock", new DistributedLockAcquisitionOptions
                            {
                                TimeModeHandler = LockTimeModeHandler.InfiniteRenewal(policy.LockExpiry),
                                RetryStrategy = new DefaultLockRetryStrategy(0)
                            }, waiting.Token);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            Record("lock_error");
                            if (!policy.BypassOnCacheFailure)
                            {
                                logger.LogError(ex, "方法缓存获取分布式锁失败，拒绝回源。");
                                throw;
                            }
                            logger.LogWarning(ex, "方法缓存获取分布式锁失败，使用进程内互斥回源。");
                            queryStarted = true;
                            await LoadAsync<T>(context, next, key, policy, callerToken, false);
                            return;
                        }

                        try
                        {
                            if (handle.Acquired)
                            {
                                var inside = await ReadAsync<T>(key, policy, callerToken);
                                if (ReturnHit(context, inside.Entry)) return;
                                queryStarted = true;
                                await LoadAsync<T>(context, next, key, policy, callerToken, !inside.Failed, handle);
                                return;
                            }
                        }
                        finally
                        {
                            try { await handle.DisposeAsync(); }
                            catch (Exception ex)
                            {
                                Record("release_error");
                                logger.LogError(ex, "方法缓存分布式锁释放失败，将依赖租约过期。");
                            }
                        }
                        await Task.Delay(policy.LockPollInterval, waiting.Token);
                    }
                }
                catch (OperationCanceledException) when (!queryStarted && !callerToken.IsCancellationRequested && waiting.IsCancellationRequested)
                {
                    Record("lock_timeout");
                    if (!policy.BypassOnLockTimeout) throw new TimeoutException("等待分布式缓存加载超时。");
                    await LoadAsync<T>(context, next, key, policy, callerToken, false);
                }
            }
        }
        finally { CurrentCall.Value = previous; }
    }

    private async Task LoadAsync<T>(AspectContext context, AspectDelegate next, string key,
        MethodCachePolicy policy, CancellationToken callerToken, bool write, ILockHandler lockHandle = null)
    {
        Record("load");
        using var query = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        query.CancelAfter(policy.QueryTimeout);
        var parameters = context.ServiceMethod.GetParameters();
        var tokenIndices = Enumerable.Range(0, parameters.Length)
            .Where(i => parameters[i].ParameterType == typeof(CancellationToken)).ToArray();
        var originalTokens = tokenIndices.Select(i => context.Parameters[i]).ToArray();
        T result;
        try
        {
            foreach (var index in tokenIndices) context.Parameters[index] = query.Token;
            // next 只执行一次。等待查询真正完成，再释放锁；不遗留使用已释放 DI 作用域的后台查询。
            await next(context);
            result = await (Task<T>)context.ReturnValue;
            query.Token.ThrowIfCancellationRequested();
        }
        finally
        {
            for (var i = 0; i < tokenIndices.Length; i++) context.Parameters[tokenIndices[i]] = originalTokens[i];
        }
        if (!write || (result is null && !policy.CacheNull)) return;
        if (lockHandle != null && !lockHandle.Acquired)
        {
            Record("lease_lost");
            logger.LogWarning("方法缓存加载期间丢失分布式锁，返回结果但跳过缓存回填。");
            return;
        }
        var expiry = result is null ? policy.NullExpiry : policy.Expiry;
        expiry = TimeSpan.FromTicks(Math.Max(1, (long)(expiry.Ticks * (1 - Random.Shared.NextDouble() * policy.ExpiryJitterRatio))));
        try
        {
            if (!await cache.SetAsync(key, new CacheEnvelope<T> { Value = result }, expiry))
            {
                Record("write_error");
                logger.LogWarning("方法缓存写入返回失败，返回已完成的查询结果。");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Record("write_error");
            logger.LogWarning(ex, "方法缓存写入失败，返回已完成的查询结果。");
        }
    }

    private async Task<(CacheEnvelope<T> Entry, bool Failed)> ReadAsync<T>(string key, MethodCachePolicy policy, CancellationToken token)
    {
        try { return (await cache.GetAsync<CacheEnvelope<T>>(key).WaitAsync(token), false); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Record("read_error");
            if (!policy.BypassOnCacheFailure)
            {
                logger.LogError(ex, "方法缓存读取失败，拒绝回源。");
                throw;
            }
            logger.LogWarning(ex, "方法缓存读取失败，允许进程内互斥降级。");
            return (null, true);
        }
    }

    private static bool ReturnHit<T>(AspectContext context, CacheEnvelope<T> entry)
    {
        if (entry == null) return false;
        Record("hit");
        context.ReturnValue = Task.FromResult(entry.Value);
        return true;
    }

    public async Task EvictAsync(AspectContext context, AspectDelegate next, CacheEvictAttribute attribute)
    {
        if (attribute.KeyParameterIndex < 0) throw new ArgumentException("CacheEvict 必须指定业务 Key 参数。");
        var policy = GetPolicy(attribute.Policy);
        var key = keys.Generate(context, attribute.Region, attribute.KeyParameterIndex, policy, GetScope(context, policy));
        var registry = context.ServiceProvider.GetService<ITransactionCallbackRegistry>();
        await next(context);
        async Task Remove()
        {
            try
            {
                await cache.RemoveAsync(key); // false 也可能仅表示 Key 已不存在。
                Record("evict");
            }
            catch (Exception ex)
            {
                Record("evict_error");
                logger.LogError(ex, "方法缓存提交后失效失败，缓存将依赖 TTL 过期。");
                throw;
            }
        }
        if (registry != null) await registry.RegisterAfterCommitAsync(Remove);
        else await Remove();
    }

    private MethodCachePolicy GetPolicy(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !options.Value.Policies.TryGetValue(name, out var policy))
            throw new InvalidOperationException($"未配置方法缓存策略 '{name}'。");
        return policy;
    }

    private static MethodCacheScope GetScope(AspectContext context, MethodCachePolicy policy)
    {
        var scope = new MethodCacheScope();
        var user = context.ServiceProvider.GetService<ICurrentUser>();
        var tenant = context.ServiceProvider.GetService<ICurrentTenant>() ?? user;
        scope.Dimensions["tenant"] = tenant?.GetTenantId();
        if (policy.VaryByUser)
        {
            scope.Dimensions["user"] = user?.GetUserId<string>();
            scope.Dimensions["roles"] = user?.GetRoles()?.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
        if (policy.VaryByCulture)
        {
            scope.Dimensions["culture"] = CultureInfo.CurrentCulture.Name;
            scope.Dimensions["ui-culture"] = CultureInfo.CurrentUICulture.Name;
        }
        foreach (var contributor in context.ServiceProvider.GetServices<IMethodCacheScopeContributor>())
            contributor.Contribute(scope);
        return scope;
    }

    private static void Record(string outcome) => Events.Add(1, new KeyValuePair<string, object>("outcome", outcome));
    private sealed record CallFrame(string Key, CallFrame Parent);
}

/// <summary>对象存在即命中；Value 可以是 null、0 或 false。</summary>
internal sealed class CacheEnvelope<T>
{
    public T Value { get; set; }
}

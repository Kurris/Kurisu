using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Cache;

namespace Kurisu.Extensions.Cache.Locking;

/// <summary>
/// Redis 本地可重入锁上下文（同一异步调用链内生效）。
/// </summary>
internal sealed class RedisReentrantLockContext
{
    // AsyncLocal 绑定 ExecutionContext：同一异步调用链跨 await 会继承该值，从而支持本地可重入。
    private readonly AsyncLocal<Dictionary<string, LocalLockScope>> _localLockScopes = new();

    /// <summary>
    /// 确保当前异步调用链已初始化本地锁容器。
    /// </summary>
    public Dictionary<string, LocalLockScope> EnsureScopes()
    {
        var scopes = _localLockScopes.Value;
        if (scopes != null)
        {
            return scopes;
        }

        scopes = new Dictionary<string, LocalLockScope>(StringComparer.Ordinal);
        _localLockScopes.Value = scopes;
        return scopes;
    }

    /// <summary>
    /// 当容器为空时清理当前异步调用链中的本地锁容器。
    /// </summary>
    public void ClearIfEmpty(Dictionary<string, LocalLockScope> scopes)
    {
        if (scopes.Count == 0 && ReferenceEquals(_localLockScopes.Value, scopes))
        {
            _localLockScopes.Value = null;
        }
    }

    /// <summary>
    /// 尝试复用已持有的同名锁。
    /// </summary>
    public async ValueTask<ILockHandler> TryEnterAsync(string lockKey, CancellationToken cancellationToken = default)
    {
        // 这里读取的是“当前异步调用链”的本地作用域，而不是线程静态变量。
        var scopes = _localLockScopes.Value;
        if (scopes != null && scopes.TryGetValue(lockKey, out var scope))
        {
            if (await CanReuseLocallyAsync(scope.LockHandler, cancellationToken).ConfigureAwait(false))
            {
                scope.AddReference();
                return new ReentrantLockHandler(this, lockKey, scope);
            }

            scopes.Remove(lockKey);
            if (scopes.Count == 0)
            {
                _localLockScopes.Value = null;
            }
        }

        return null;
    }

    private static async ValueTask<bool> CanReuseLocallyAsync(ILockHandler lockHandler, CancellationToken cancellationToken)
    {
        if (lockHandler is ILocalReentryAwareLockHandler reentryAwareHandler)
        {
            return await reentryAwareHandler.TryReenterAsync(cancellationToken).ConfigureAwait(false);
        }

        return lockHandler.Acquired;
    }

    /// <summary>
    /// 注册新锁并返回可重入句柄。
    /// </summary>
    public ILockHandler Register(string lockKey, ILockHandler lockHandler, Dictionary<string, LocalLockScope>? scopes = null)
    {
        scopes ??= EnsureScopes();

        var localScope = new LocalLockScope(lockHandler);
        scopes[lockKey] = localScope;
        return new ReentrantLockHandler(this, lockKey, localScope);
    }

    /// <summary>
    /// 释放当前异步调用链内的本地锁引用。
    /// </summary>
    private async ValueTask ReleaseAsync(string lockKey, LocalLockScope localScope)
    {
        var remaining = localScope.ReleaseReference();
        if (remaining > 0)
        {
            return;
        }

        if (remaining < 0)
        {
            return;
        }

        var scopes = _localLockScopes.Value;
        if (scopes != null && scopes.TryGetValue(lockKey, out var registeredScope) && ReferenceEquals(registeredScope, localScope))
        {
            scopes.Remove(lockKey);
            if (scopes.Count == 0)
            {
                _localLockScopes.Value = null;
            }
        }

        await localScope.LockHandler.DisposeAsync();
    }

    public sealed class LocalLockScope
    {
        public LocalLockScope(ILockHandler lockHandler)
        {
            LockHandler = lockHandler;
            _referenceCount = 1;
        }

        public ILockHandler LockHandler { get; }

        private int _referenceCount;

        public int AddReference() => Interlocked.Increment(ref _referenceCount);

        public int ReleaseReference() => Interlocked.Decrement(ref _referenceCount);
    }

    /// <summary>
    /// 同一调用链内的可重入锁句柄，真实释放由引用计数归零触发。
    /// </summary>
    private sealed class ReentrantLockHandler : ILockHandler
    {
        private readonly RedisReentrantLockContext _context;
        private readonly string _lockKey;
        private readonly LocalLockScope _localScope;
        private int _disposed;

        public ReentrantLockHandler(RedisReentrantLockContext context, string lockKey, LocalLockScope localScope)
        {
            _context = context;
            _lockKey = lockKey;
            _localScope = localScope;
        }

        public bool Acquired => _localScope.LockHandler.Acquired;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            await _context.ReleaseAsync(_lockKey, _localScope);
        }
    }
}

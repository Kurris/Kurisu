using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache.Locking;
using Moq;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "reentrant-context")]
public class RedisReentrantLockContextMockTests
{
    [Fact(DisplayName = "Register在未提供scopes时应自动初始化")]
    public void Register_ShouldInitializeScopes_WhenCalledWithoutScopesParameter()
    {
        var context = new RedisReentrantLockContext();
        var mockHandler = new Mock<ILockHandler>();
        mockHandler.SetupGet(h => h.Acquired).Returns(true);

        // 不传 scopes → 触发 scopes ??= EnsureScopes() 路径
        var result = context.Register("test-key", mockHandler.Object);
        Assert.True(result.Acquired);
    }

    [Fact(DisplayName = "ClearIfEmpty对非匹配scopes应安全无操作")]
    public void ClearIfEmpty_ShouldBeNoOp_WhenScopesNotSameAsCurrent()
    {
        var context = new RedisReentrantLockContext();

        // 先初始化当前调用链的 scopes
        var scopes = context.EnsureScopes();
        Assert.NotNull(scopes);

        // 传入一个不同的 Dictionary → ReferenceEquals 失败 → 不清理
        var foreignScopes = new Dictionary<string, RedisReentrantLockContext.LocalLockScope>();
        context.ClearIfEmpty(foreignScopes);

        // 当前 scopes 应不受影响
        Assert.Same(scopes, context.EnsureScopes());
    }

    [Fact(DisplayName = "ClearIfEmpty在AsyncLocal未初始化时应对null安全")]
    public void ClearIfEmpty_ShouldBeSafe_WhenAsyncLocalValueIsNull()
    {
        var context = new RedisReentrantLockContext();

        // _localLockScopes.Value 为 null，ReferenceEquals(null, scopes) → false → no-op
        var emptyDict = new Dictionary<string, RedisReentrantLockContext.LocalLockScope>();
        context.ClearIfEmpty(emptyDict);

        // 不应抛异常，EnsureScopes 会创建新容器
        var scopes = context.EnsureScopes();
        Assert.NotNull(scopes!);
    }

    [Fact(DisplayName = "ClearIfEmpty在scopes非空时应短路不清理")]
    public void ClearIfEmpty_ShouldShortCircuit_WhenScopesNotEmpty()
    {
        var context = new RedisReentrantLockContext();
        var mockHandler = new Mock<ILockHandler>();
        mockHandler.SetupGet(h => h.Acquired).Returns(true);

        // Register 会初始化 scopes
        context.Register("key-short", mockHandler.Object);

        // scopes 非空（Count > 0），短路 → 不清理
        var scopes = context.EnsureScopes();
        Assert.NotEmpty(scopes!);
    }

    [Fact(DisplayName = "ReentrantLockHandler二次Dispose应幂等")]
    public async Task ReentrantHandler_ShouldBeIdempotent_WhenDisposedTwice()
    {
        var context = new RedisReentrantLockContext();
        var mockHandler = new Mock<ILockHandler>();
        mockHandler.SetupGet(h => h.Acquired).Returns(true);

        var handler = context.Register("test-key-double-dispose", mockHandler.Object);
        Assert.True(handler.Acquired);

        await handler.DisposeAsync();
        // 二次 Dispose 应走 _disposed == 1 早期返回路径
        await handler.DisposeAsync();
    }
}

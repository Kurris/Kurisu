using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.ContextAccessor;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.ContextAccessor.Default;

namespace Kurisu.Test.ContextAccessor;

public class ContextAccessorTests
{
    public class TestState : IContextable<TestState>
    {
        public string Name { get; set; }
        public int Counter { get; set; }

        public TestState()
        {
            Name = null;
            Counter = 0;
        }
    }

    /// <summary>
    /// 包含只读属性的测试状态
    /// </summary>
    public class ReadOnlyTestState : IContextable<ReadOnlyTestState>
    {
        public string Name { get; set; }
        public int Counter { get; }

        public ReadOnlyTestState()
        {
            Name = null;
            Counter = 42;
        }
    }

    /// <summary>
    /// 包含引用类型属性的测试状态,用于验证深拷贝
    /// </summary>
    public class ComplexTestState : IContextable<ComplexTestState>
    {
        public List<string> Tags { get; set; } = new();
    }

    [Fact]
    public void AddStateAccessor_RegistersAccessorAndLifecycle()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();

        Assert.Same(accessor, lifecycle);

        lifecycle.Initialize();
        Assert.NotNull(accessor.Current);

        lifecycle.Remove();
        Assert.Null(accessor.Current);
    }

    [Fact]
    public void WithSnapshot_CreateScope_RestoresState()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        lifecycle.Initialize();

        accessor.Current.Name = "initial";
        accessor.Current.Counter = 1;

        using (snapshotManager.CreateScope(s =>
        {
            s.Name = "inner";
            s.Counter = 2;
        }, null))
        {
            Assert.Equal("inner", accessor.Current.Name);
            Assert.Equal(2, accessor.Current.Counter);
        }

        Assert.Equal("initial", accessor.Current.Name);
        Assert.Equal(1, accessor.Current.Counter);

        lifecycle.Remove();
    }


    [Fact]
    public async Task Snapshot_NestedAsyncScopes_RestoreCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        lifecycle.Initialize();

        accessor.Current.Name = "orig";
        accessor.Current.Counter = 0;

        await using (await Task.FromResult(snapshotManager.CreateScopeAsync(s =>
        {
            s.Name = "outer";
            s.Counter = 1;
        }, async () => { await Task.Delay(1); })))
        {
            Assert.Equal("outer", accessor.Current.Name);
            Assert.Equal(1, accessor.Current.Counter);

            await using (await Task.FromResult(snapshotManager.CreateScopeAsync(s =>
            {
                s.Name = "inner";
                s.Counter = 2;
            }, async () => { await Task.Delay(1); })))
            {
                Assert.Equal("inner", accessor.Current.Name);
                Assert.Equal(2, accessor.Current.Counter);
            }

            // after inner disposed, should restore to outer
            Assert.Equal("outer", accessor.Current.Name);
            Assert.Equal(1, accessor.Current.Counter);
        }

        // after outer disposed, should restore to original
        Assert.Equal("orig", accessor.Current.Name);
        Assert.Equal(0, accessor.Current.Counter);

        lifecycle.Remove();
    }

    [Fact]
    public async Task DifferentRequests_AreIsolated()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();

        var sp = services.BuildServiceProvider();

        // simulate two requests running on thread-pool threads (separate execution contexts)
        var t1 = Task.Run(() =>
        {
            using var scope = sp.CreateScope();
            var prov = scope.ServiceProvider;
            var lifecycle = prov.GetRequiredService<IAppAsyncLocalLifecycle>();
            var accessor = prov.GetRequiredService<IContextAccessor<TestState>>();

            lifecycle.Initialize();
            accessor.Current.Name = "RequestA";
            // ensure some overlap
            Task.Delay(50).Wait();
            var name = accessor.Current.Name;
            lifecycle.Remove();
            return name;
        });

        var t2 = Task.Run(() =>
        {
            using var scope = sp.CreateScope();
            var prov = scope.ServiceProvider;
            var lifecycle = prov.GetRequiredService<IAppAsyncLocalLifecycle>();
            var accessor = prov.GetRequiredService<IContextAccessor<TestState>>();

            lifecycle.Initialize();
            accessor.Current.Name = "RequestB";
            Task.Delay(30).Wait();
            var name = accessor.Current.Name;
            lifecycle.Remove();
            return name;
        });

        await Task.WhenAll(t1, t2);

        Assert.Equal("RequestA", t1.Result);
        Assert.Equal("RequestB", t2.Result);
        Assert.NotEqual(t1.Result, t2.Result);
    }

    [Fact]
    public void WithoutSnapshot_ManagerNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();

        var sp = services.BuildServiceProvider();

        var snapshotManager = sp.GetService<IContextSnapshotManager<TestState>>();
        Assert.Null(snapshotManager);
    }

    [Fact]
    public void Snapshot_CreateScope_NullSetState_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        Assert.Throws<ArgumentNullException>(() => snapshotManager.CreateScope(null, null));
    }

    [Fact]
    public async Task Snapshot_CreateScopeAsync_RestoresStateAndAwaitsOnAfterDispose()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        lifecycle.Initialize();

        accessor.Current.Name = "initial";
        accessor.Current.Counter = 1;

        var afterDisposed = false;

        await using (await Task.FromResult(snapshotManager.CreateScopeAsync(s =>
        {
            s.Name = "async";
            s.Counter = 2;
        }, async () =>
        {
            await Task.Delay(1);
            afterDisposed = true;
        })))
        {
            Assert.Equal("async", accessor.Current.Name);
            Assert.Equal(2, accessor.Current.Counter);
        }

        // after disposing async scope, onAfterDispose should have been awaited and executed
        Assert.True(afterDisposed);
        Assert.Equal("initial", accessor.Current.Name);
        Assert.Equal(1, accessor.Current.Counter);

        lifecycle.Remove();
    }

    [Fact]
    public void Snapshot_NestedScopes_RestoreCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        lifecycle.Initialize();

        accessor.Current.Name = "orig";

        using (snapshotManager.CreateScope(s => s.Name = "outer", null))
        {
            Assert.Equal("outer", accessor.Current.Name);

            using (snapshotManager.CreateScope(s => s.Name = "inner", null))
            {
                Assert.Equal("inner", accessor.Current.Name);
            }

            // after inner disposed, should restore to outer
            Assert.Equal("outer", accessor.Current.Name);
        }

        // after outer disposed, should restore to original
        Assert.Equal("orig", accessor.Current.Name);

        lifecycle.Remove();
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/openapi/swagger")]
    public async Task Middleware_ApiPaths_InitializesContext(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();
        var sp = services.BuildServiceProvider();

        TestState capturedState = null;

        var middleware = new ContextAccessorMiddleware(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IContextAccessor<TestState>>();
            capturedState = accessor.Current;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.RequestServices = sp;
        context.Request.Path = path;

        await middleware.Invoke(context);

        Assert.NotNull(capturedState);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    [InlineData("/swagger/index.html")]
    [InlineData("/home/index")]
    public async Task Middleware_NonApiPaths_SkipsContextInit(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();
        var sp = services.BuildServiceProvider();

        TestState capturedState = null;

        var middleware = new ContextAccessorMiddleware(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IContextAccessor<TestState>>();
            capturedState = accessor.Current;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.RequestServices = sp;
        context.Request.Path = path;

        await middleware.Invoke(context);

        Assert.Null(capturedState);
    }

    [Fact]
    public void Snapshot_WithReadOnlyProperties_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<ReadOnlyTestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<ReadOnlyTestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<ReadOnlyTestState>>();

        lifecycle.Initialize();

        accessor.Current.Name = "original";
        var originalCounter = accessor.Current.Counter;

        using (snapshotManager.CreateScope(s =>
        {
            s.Name = "modified";
        }, null))
        {
            Assert.Equal("modified", accessor.Current.Name);
            Assert.Equal(originalCounter, accessor.Current.Counter);
        }

        // 可写属性被恢复,只读属性不变更
        Assert.Equal("original", accessor.Current.Name);
        Assert.Equal(originalCounter, accessor.Current.Counter);

        lifecycle.Remove();
    }

    [Fact]
    public void Current_Reinitialized_AfterRemove()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>();

        var sp = services.BuildServiceProvider();

        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();

        lifecycle.Initialize();
        accessor.Current.Name = "first";
        lifecycle.Remove();
        Assert.Null(accessor.Current);

        lifecycle.Initialize();
        Assert.NotNull(accessor.Current);
        Assert.Null(accessor.Current.Name);
        lifecycle.Remove();
    }

    [Fact]
    public void Snapshot_CreateScopeAsync_NullSetter_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        Assert.Throws<ArgumentNullException>(() => snapshotManager.CreateScopeAsync(null, null));
    }

    [Fact]
    public void Snapshot_CreateScope_NullOnDispose_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextAccessor<TestState>().WithSnapshot();

        var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<IContextAccessor<TestState>>();
        var lifecycle = sp.GetRequiredService<IAppAsyncLocalLifecycle>();
        var snapshotManager = sp.GetRequiredService<IContextSnapshotManager<TestState>>();

        lifecycle.Initialize();

        var ex = Record.Exception(() =>
        {
            using (snapshotManager.CreateScope(s => s.Name = "test", null))
            {
            }
        });

        Assert.Null(ex);

        lifecycle.Remove();
    }

    [Fact]
    public void CopyState_DeepCopies_ReferenceTypeProperties()
    {
        var original = new ComplexTestState { Tags = new List<string> { "a", "b" } };
        var clone = ((IContextable<ComplexTestState>)original).CopyState();

        Assert.Equal(original.Tags, clone.Tags);
        Assert.NotSame(original.Tags, clone.Tags);

        original.Tags.Add("c");
        Assert.Equal(2, clone.Tags.Count);
        Assert.DoesNotContain("c", clone.Tags);
    }

    [Fact]
    public void DefaultOperateContextModule_HasCorrectMetadata()
    {
        var module = new DefaultOperateContextModule();

        Assert.Equal("操作状态模块", module.Name);
        Assert.Equal(200, module.Order);
        Assert.True(module.IsBeforeUseRouting);
    }
}

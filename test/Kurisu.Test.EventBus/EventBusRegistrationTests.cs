using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.EventBus;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Defaults;
using Kurisu.Extensions.EventBus.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Kurisu.Test.EventBus;

public class EventBusRegistrationTests
{
    [Fact]
    public void AddEventBus_RegistersDefaultRuntimeChain()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(DispatchProxy.Create<IDbContext, NoopDbContextProxy>());
        services.AddScoped<ITransactionCallbackRegistry, TestTransactionCallbackRegistry>();
        services.AddEventBus();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventBus>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventMessageProcessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventMessageDispatcher>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventBusDeadLetterService>());
    }

    [Fact]
    public async Task CustomSignal_IsSharedByPublisherAndRetryService()
    {
        var signal = new TestDispatchSignal();
        var callbacks = new TestTransactionCallbackRegistry();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEventBusDispatchSignal>(signal);
        services.AddSingleton<ITransactionCallbackRegistry>(callbacks);
        services.AddScoped<ILocalMessageStore, TestLocalMessageStore>();
        services.AddEventBus();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var retry = provider.GetServices<IHostedService>().OfType<LocalMessageRetryBackgroundService>().Single();
        await retry.StartAsync(CancellationToken.None);
        try
        {
            await signal.WaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
#pragma warning disable KS1001 // 验证注册及通知链，不使用数据库事务代理
            await bus.PublishAsync(new TestMessage());
#pragma warning restore KS1001
            Assert.Equal(0, signal.NotifyCount);
            await callbacks.ExecuteAsync();
            Assert.Equal(1, signal.NotifyCount);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await retry.StopAsync(timeout.Token);
        }
    }

    [Fact]
    public void Serializer_DoesNotPersistProcessingToken()
    {
        var serializer = new DefaultEventBusSerializer();
        var message = new TestMessage
        {
            Code = "code",
            ProcessingToken = "internal-token",
            Name = "test"
        };

        var content = serializer.Serialize(message);
        var restored = serializer.Deserialize<EventMessage>(content);

        Assert.DoesNotContain("internal-token", content);
        Assert.Equal("code", restored.Code);
        Assert.Null(restored.ProcessingToken);
    }

    [Fact]
    public void Serializer_RejectsNonEventMessageTypes()
    {
        var serializer = new DefaultEventBusSerializer();
        const string content = """{"$type":"System.Version, System.Private.CoreLib","Major":1}""";

        Assert.Throws<JsonSerializationException>(() => serializer.Deserialize<EventMessage>(content));
    }

    [Fact]
    public async Task PublishAsync_NotifiesOnlyAfterTransactionCallbackRuns()
    {
        var signal = new TestDispatchSignal();
        var callbacks = new TestTransactionCallbackRegistry();
        var bus = new DefaultEventBus(new TestLocalMessageStore(), signal, callbacks);
        var message = new TestMessage { Name = "test" };

#pragma warning disable KS1001 // 测试内部调用，无需真实事务
        await bus.PublishAsync(message);
#pragma warning restore KS1001

        Assert.Equal("test-code", message.Code);
        Assert.Equal(0, signal.NotifyCount);

        await callbacks.ExecuteAsync();

        Assert.Equal(1, signal.NotifyCount);
    }

    [Fact]
    public async Task Dispatcher_UsesRuntimeMessageTypeAndInvokesAllHandlers()
    {
        var calls = new List<int>();
        var services = new ServiceCollection();
        services.AddScoped<IEventMessageHandler<TestMessage>>(_ => new RecordingHandler(calls, 1));
        services.AddScoped<IEventMessageHandler<TestMessage>>(_ => new RecordingHandler(calls, 2));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = new DefaultEventMessageDispatcher(scope.ServiceProvider);
        EventMessage message = new TestMessage();

        await dispatcher.DispatchAsync(message, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, calls);
    }

    [Fact]
    public async Task Dispatcher_ResolvesHandlersFromEachScope()
    {
        var instances = new List<Guid>();
        var services = new ServiceCollection();
        services.AddScoped<IEventMessageHandler<TestMessage>>(_ =>
        {
            var id = Guid.NewGuid();
            return new DelegateHandler((_, _) =>
            {
                instances.Add(id);
                return Task.CompletedTask;
            });
        });
        using var provider = services.BuildServiceProvider(validateScopes: true);
        for (var i = 0; i < 2; i++)
        {
            using var scope = provider.CreateScope();
            var dispatcher = new DefaultEventMessageDispatcher(scope.ServiceProvider);
            await dispatcher.DispatchAsync(new TestMessage(), CancellationToken.None);
            await dispatcher.DispatchAsync(new TestMessage(), CancellationToken.None);
        }

        Assert.Equal(4, instances.Count);
        Assert.Equal(instances[0], instances[1]);
        Assert.Equal(instances[2], instances[3]);
        Assert.NotEqual(instances[0], instances[2]);
    }

    [Fact]
    public async Task Dispatcher_AwaitsHandlerAndStopsOnFailure()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextCalled = false;
        var error = new InvalidOperationException("handler failure");
        using var cancellation = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddScoped<IEventMessageHandler<TestMessage>>(_ => new DelegateHandler(async (_, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            entered.SetResult();
            await release.Task;
            throw error;
        }));
        services.AddScoped<IEventMessageHandler<TestMessage>>(_ => new DelegateHandler((_, _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = new DefaultEventMessageDispatcher(scope.ServiceProvider);
        var dispatch = dispatcher.DispatchAsync(new TestMessage(), cancellation.Token);
        await entered.Task;
        Assert.False(dispatch.IsCompleted);
        Assert.False(nextCalled);
        release.SetResult();

        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => dispatch));
        Assert.False(nextCalled);
    }

    private sealed class DelegateHandler(Func<TestMessage, CancellationToken, Task> handle)
        : IEventMessageHandler<TestMessage>
    {
        public Task HandleAsync(TestMessage message, CancellationToken cancellationToken)
            => handle(message, cancellationToken);
    }

    private sealed class RecordingHandler(List<int> calls, int id) : IEventMessageHandler<TestMessage>
    {
        public Task HandleAsync(TestMessage message, CancellationToken cancellationToken)
        {
            calls.Add(id);
            return Task.CompletedTask;
        }
    }

    private sealed class TestMessage : EventMessage
    {
        public string Name { get; set; }
    }

    private class NoopDbContextProxy : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestLocalMessageStore : ILocalMessageStore
    {
        public Task<string> PersistAsync<TMessage>(TMessage message) where TMessage : EventMessage
        {
            message.Code = "test-code";
            return Task.FromResult("test-code");
        }

        public Task<string> TryClaimAsync(string code, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ILocalMessageTracker> BeginTrackingAsync(
            string code,
            string processingToken,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task FailDeliveryAsync(
            string code,
            string processingToken,
            string error,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestDispatchSignal : IEventBusDispatchSignal
    {
        public int NotifyCount { get; private set; }
        public TaskCompletionSource WaitStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            WaitStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return false;
        }

        public void Notify()
        {
            NotifyCount++;
        }
    }

    private sealed class TestTransactionCallbackRegistry : ITransactionCallbackRegistry
    {
        private readonly List<Func<Task>> _callbacks = [];

        public Task RegisterAfterCommitAsync(Func<Task> callback)
        {
            _callbacks.Add(callback);
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync()
        {
            foreach (var callback in _callbacks)
            {
                await callback();
            }
        }
    }
}

using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.EventBus;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Defaults;
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
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventBusMessageHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventBusMessageServiceHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEventBusDeadLetterService>());
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
        var bus = new DefaultEventBus(new TestLocalMessageHandler(), signal, callbacks);
        var message = new TestMessage { Name = "test" };

#pragma warning disable KS1001 // 测试内部调用，无需真实事务
        await bus.PublishAsync(message);
#pragma warning restore KS1001

        Assert.Equal("test-code", message.Code);
        Assert.Equal(0, signal.NotifyCount);

        await callbacks.ExecuteAsync();

        Assert.Equal(1, signal.NotifyCount);
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

    private sealed class TestLocalMessageHandler : IEventBusLocalMessageHandler
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

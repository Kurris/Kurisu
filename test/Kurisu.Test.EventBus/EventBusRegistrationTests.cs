using System.Reflection;
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
}

using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Kurisu.Extensions.EventBus.Options;

namespace Kurisu.Test.EventBus;

[Collection("EventBusDatabase")]
public class EventBusDatabaseTests
{
    [Fact]
    public async Task TryClaimAsync_AllowsOnlyOneConcurrentConsumer()
    {
        using var provider = CreateProvider();
        var code = await PersistAsync(provider);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ClaimAsync(provider, code)));

        Assert.Single(tokens.Where(x => !string.IsNullOrEmpty(x)));
    }

    [Fact]
    public async Task DeadLetter_CanBeQueriedAndIgnoredManually()
    {
        using var provider = CreateProvider(maxAttemptCount: 1);
        var code = await PersistAsync(provider);
        var token = await ClaimAsync(provider, code);

        await InScopeAsync(provider, async services =>
        {
            var handler = services.GetRequiredService<IEventBusLocalMessageHandler>();
            await handler.FailDeliveryAsync(code, token, "test failure");
        });

        await InScopeAsync(provider, async services =>
        {
            var deadLetterService = services.GetRequiredService<IEventBusDeadLetterService>();
            var deadLetter = await deadLetterService.GetAsync(code);
            Assert.Equal(1, services.GetRequiredService<IOptions<EventBusOptions>>().Value.MaxAttemptCount);
            Assert.Equal(1, deadLetter.Attempts);
            Assert.Equal(LocalMessageStatus.DeadLetter, deadLetter.Status);

            await deadLetterService.IgnoreAsync(code, "verified by test");
            var ignored = await deadLetterService.GetAsync(code);
            Assert.Equal(LocalMessageStatus.Ignored, ignored.Status);
            Assert.Equal("verified by test", ignored.DispositionReason);
        });
    }

    private static ServiceProvider CreateProvider(int maxAttemptCount = 5)
    {
        return (ServiceProvider)TestHelper.GetServiceProvider(configureServices: services =>
        {
            services.AddEventBus(options =>
            {
                options.MaxAttemptCount = maxAttemptCount;
            });
        });
    }

    private static async Task<string> PersistAsync(IServiceProvider provider)
    {
        return await InScopeAsync(provider, async services =>
        {
            var db = services.GetRequiredService<IDbContext>();
            db.CodeFirst.EnsureTableExists(typeof(LocalMessage));

            var handler = services.GetRequiredService<IEventBusLocalMessageHandler>();
            return await handler.PersistAsync(new DatabaseTestMessage { Name = Guid.NewGuid().ToString() });
        });
    }

    private static Task<string> ClaimAsync(IServiceProvider provider, string code)
    {
        return InScopeAsync(provider, services =>
        {
            var handler = services.GetRequiredService<IEventBusLocalMessageHandler>();
            return handler.TryClaimAsync(code);
        });
    }

    private static async Task InScopeAsync(IServiceProvider provider, Func<IServiceProvider, Task> action)
    {
        await InScopeAsync(provider, async services =>
        {
            await action(services);
            return true;
        });
    }

    private static async Task<T> InScopeAsync<T>(IServiceProvider provider, Func<IServiceProvider, Task<T>> action)
    {
        using var scope = provider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (db.CreateDatasourceScope())
            {
                return await action(scope.ServiceProvider);
            }
        }
    }

    private sealed class DatabaseTestMessage : EventMessage
    {
        public string Name { get; set; }
    }
}

[CollectionDefinition("EventBusDatabase", DisableParallelization = true)]
public class EventBusDatabaseCollection;

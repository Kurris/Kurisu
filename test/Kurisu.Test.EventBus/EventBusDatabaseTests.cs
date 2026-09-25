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
            var handler = services.GetRequiredService<ILocalMessageStore>();
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("failure")]
    public async Task Tracker_FailureWinsRegardlessOfErrorText(string error)
    {
        using var provider = CreateProvider();
        var code = await PersistAsync(provider);
        var token = await ClaimAsync(provider, code);

        await InScopeAsync(provider, async services =>
        {
            var store = services.GetRequiredService<ILocalMessageStore>();
            using var cancellation = new CancellationTokenSource();
            var tracker = await store.BeginTrackingAsync(code, token, cancellation.Token);
            Assert.NotNull(tracker);
            tracker.Complete();
            tracker.Fail(error);
            tracker.Complete();
            await tracker.DisposeAsync();

            // A second disposal must not issue another database operation.
            cancellation.Cancel();
            await tracker.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => tracker.Complete());
            Assert.Throws<ObjectDisposedException>(() => tracker.Fail("late failure"));
        });

        // 验证持久化结果使用新 Scope，避免复用已接收取消令牌的 ORM 实例。
        await InScopeAsync(provider, async services =>
        {
            var message = await services.GetRequiredService<IDbContext>()
                .Queryable<LocalMessage>().SingleAsync(x => x.Code == code);
            Assert.Equal(LocalMessageStatus.Pending, message.Status);
            Assert.Null(message.ProcessingToken);
            Assert.NotNull(message.NextRetryTime);
        });
    }

    [Fact]
    public async Task Tracker_StaleTokenCannotCompleteNewClaim()
    {
        using var provider = CreateProvider();
        var code = await PersistAsync(provider);
        var token = await ClaimAsync(provider, code);

        await InScopeAsync(provider, async services =>
        {
            var store = services.GetRequiredService<ILocalMessageStore>();
            var db = services.GetRequiredService<IDbContext>();
            var tracker = await store.BeginTrackingAsync(code, token);
            await db.AsSqlSugarDbContext().Updateable<LocalMessage>()
                .SetColumns(x => x.LockedUntil == DateTime.Now.AddMinutes(-1))
                .Where(x => x.Code == code).ExecuteCommandAsync();
            var nextToken = await store.TryClaimAsync(code);
            Assert.NotNull(nextToken);
            Assert.NotEqual(token, nextToken);

            tracker.Complete();
            await tracker.DisposeAsync();
            var message = await db.Queryable<LocalMessage>().SingleAsync(x => x.Code == code);
            Assert.Equal(LocalMessageStatus.Processing, message.Status);
            Assert.Equal(nextToken, message.ProcessingToken);
        });
    }

    [Fact]
    public async Task TrackingQueries_RespectCancellation()
    {
        using var provider = CreateProvider();
        await InScopeAsync(provider, async services =>
        {
            var store = services.GetRequiredService<ILocalMessageStore>();
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.BeginTrackingAsync("code", "token", canceled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.FailDeliveryAsync("code", "token", "error", canceled.Token));
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

            var handler = services.GetRequiredService<ILocalMessageStore>();
            return await handler.PersistAsync(new DatabaseTestMessage { Name = Guid.NewGuid().ToString() });
        });
    }

    private static Task<string> ClaimAsync(IServiceProvider provider, string code)
    {
        return InScopeAsync(provider, services =>
        {
            var handler = services.GetRequiredService<ILocalMessageStore>();
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

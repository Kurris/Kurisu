using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Defaults;
using Kurisu.Extensions.EventBus.Internal;
using Kurisu.Extensions.EventBus.Options;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.Test.EventBus;

[Collection("EventBusDatabase")]
public class LocalMessageRetryDatabaseTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public async Task OneNotification_DeliversAllBatches(int messageCount)
    {
        await using var fixture = new Fixture();
        var codes = await fixture.SeedAsync(messageCount);
        using var service = fixture.CreateService();
        await service.StartAsync(CancellationToken.None);
        try
        {
            fixture.Signal.Notify();
            await service.Drained.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(3, service.ScanCount);
            var delivered = new List<EventMessage>();
            while (fixture.Channel.Reader.TryRead(out var message)) delivered.Add(message);
            Assert.Equal(codes.OrderBy(x => x), delivered.Select(x => x.Code).OrderBy(x => x));
            Assert.All(delivered, message => Assert.False(string.IsNullOrWhiteSpace(message.ProcessingToken)));
            var rows = await fixture.InScopeAsync(sp => sp.GetRequiredService<IDbContext>().Queryable<LocalMessage>().ToListAsync());
            Assert.Equal(messageCount, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.Equal(LocalMessageStatus.Processing, row.Status);
                Assert.Equal(1, row.Attempts);
                Assert.Equal(delivered.Single(x => x.Code == row.Code).ProcessingToken, row.ProcessingToken);
            });
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await service.StopAsync(timeout.Token);
        }
    }

    [Fact]
    public async Task FullBatchClaimedByCompetitor_StopsWithoutDelivering()
    {
        await using var fixture = new Fixture(loseClaims: true);
        await fixture.SeedAsync(2);
        using var service = fixture.CreateService();
        await service.StartAsync(CancellationToken.None);
        try
        {
            fixture.Signal.Notify();
            await service.Drained.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(1, service.ScanCount);
            Assert.Equal(2, fixture.LostClaims);
            Assert.False(fixture.Channel.Reader.TryRead(out _));
            var rows = await fixture.InScopeAsync(sp => sp.GetRequiredService<IDbContext>().Queryable<LocalMessage>().ToListAsync());
            Assert.All(rows, row =>
            {
                Assert.Equal(LocalMessageStatus.Processing, row.Status);
                Assert.Equal(1, row.Attempts);
                Assert.False(string.IsNullOrWhiteSpace(row.ProcessingToken));
            });
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await service.StopAsync(timeout.Token);
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _table = "EventBusBatchTest_" + Guid.NewGuid().ToString("N");
        private readonly ServiceProvider _provider;
        private bool _tableCreated;
        public Channel<EventMessage> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<EventMessage>();
        public IEventBusDispatchSignal Signal => _provider.GetRequiredService<IEventBusDispatchSignal>();
        public int LostClaims;

        public Fixture(bool loseClaims = false)
        {
            _provider = (ServiceProvider)TestHelper.GetServiceProvider(configureServices: services =>
            {
                // 每个测试使用独立表；所有 Scope 仍运行真实的查询、领取和状态更新。
                var clientFactory = services.Last(x => x.ServiceType == typeof(ISqlSugarClient)).ImplementationFactory;
                services.AddTransient<ISqlSugarClient>(sp =>
                {
                    var client = (ISqlSugarClient)clientFactory(sp);
                    client.MappingTables.Add(nameof(LocalMessage), _table);
                    return client;
                });
                services.AddEventBus(options =>
                {
                    options.ScanBatchSize = 2;
                    options.ScanInterval = TimeSpan.FromHours(1);
                });
                if (loseClaims)
                {
                    services.AddScoped<ILocalMessageStore>(sp => new CompetingStore(
                        ActivatorUtilities.CreateInstance<DefaultLocalMessageStore>(sp),
                        () => Interlocked.Increment(ref LostClaims)));
                }
            });
        }

        public ObservedService CreateService() => new(_provider, Channel.Writer, Signal,
            _provider.GetRequiredService<IOptions<EventBusOptions>>());

        public Task<List<string>> SeedAsync(int count) => InScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<IDbContext>();
            db.CodeFirst.EnsureTableExists(typeof(LocalMessage), _table);
            _tableCreated = true;
            var codes = new List<string>();
            for (var i = 0; i < count; i++)
                codes.Add(await sp.GetRequiredService<ILocalMessageStore>().PersistAsync(new BatchMessage()));
            return codes;
        });

        public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
        {
            using var scope = _provider.CreateScope();
            using var lifecycle = scope.ServiceProvider.InitLifecycle();
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using var datasource = db.CreateDatasourceScope();
            return await action(scope.ServiceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_tableCreated)
                    await InScopeAsync(sp => Task.FromResult(sp.GetRequiredService<IDbContext>()
                        .AsSqlSugarDbContext().GetClient().DbMaintenance.DropTable(_table)));
            }
            finally { _provider.Dispose(); }
        }
    }

    private sealed class ObservedService(IServiceProvider provider, ChannelWriter<EventMessage> writer,
        IEventBusDispatchSignal signal, IOptions<EventBusOptions> options)
        : LocalMessageRetryBackgroundService(NullLogger<LocalMessageRetryBackgroundService>.Instance,
            provider, writer, signal, options)
    {
        public int ScanCount { get; private set; }
        public TaskCompletionSource Drained { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<bool> ScanAndRetryAsync(CancellationToken stoppingToken)
        {
            ScanCount++;
            try
            {
                var more = await base.ScanAndRetryAsync(stoppingToken);
                if (!more) Drained.TrySetResult();
                return more;
            }
            catch (Exception error) { Drained.TrySetException(error); throw; }
        }
    }

    // 在扫描已读到 Pending 行之后，让竞争者先完成真实的原子领取，再执行本消费者的领取。
    private sealed class CompetingStore(ILocalMessageStore inner, Action lostClaim) : ILocalMessageStore
    {
        public Task<string> PersistAsync<T>(T message) where T : EventMessage => inner.PersistAsync(message);
        public async Task<string> TryClaimAsync(string code, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(await inner.TryClaimAsync(code, cancellationToken));
            var token = await inner.TryClaimAsync(code, cancellationToken);
            Assert.Null(token);
            lostClaim();
            return token;
        }
        public Task<ILocalMessageTracker> BeginTrackingAsync(string code, string token, CancellationToken cancellationToken = default)
            => inner.BeginTrackingAsync(code, token, cancellationToken);
        public Task FailDeliveryAsync(string code, string token, string error, CancellationToken cancellationToken = default)
            => inner.FailDeliveryAsync(code, token, error, cancellationToken);
    }

    private sealed class BatchMessage : EventMessage;
}

using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Internal;
using Kurisu.Extensions.EventBus.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.EventBus;

public class LocalMessageDispatchSignalTests
{
    [Fact]
    public async Task Notifications_AreCoalescedAndCanWakeAgainAfterTimeout()
    {
        var signal = new LocalMessageDispatchSignal();
        signal.Notify();
        signal.Notify();
        Assert.True(await signal.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
        Assert.False(await signal.WaitAsync(TimeSpan.FromMilliseconds(20), CancellationToken.None));

        var waiting = signal.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);
        signal.Notify();
        Assert.True(await waiting.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_IsPropagated(bool notifyFirst)
    {
        var signal = new LocalMessageDispatchSignal();
        using var cancellation = new CancellationTokenSource();
        if (notifyFirst)
        {
            signal.Notify();
            cancellation.Cancel();
        }

        var waiting = signal.WaitAsync(Timeout.InfiniteTimeSpan, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    [Fact]
    public async Task SingleNotification_DrainsBatchesBeforeWaitingAgain()
    {
        var signal = new LocalMessageDispatchSignal();
        using var service = new BatchService(signal);
        await service.StartAsync(CancellationToken.None);
        try
        {
            signal.Notify();
            await service.Drained.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(3, service.ScanCount);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await service.StopAsync(timeout.Token);
        }
    }

    private sealed class BatchService(IEventBusDispatchSignal signal)
        : LocalMessageRetryBackgroundService(
            NullLogger<LocalMessageRetryBackgroundService>.Instance,
            null, null, signal,
            Options.Create(new EventBusOptions { ScanInterval = TimeSpan.FromHours(1) }))
    {
        public int ScanCount { get; private set; }
        public TaskCompletionSource Drained { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task<bool> ScanAndRetryAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            ScanCount++;
            if (ScanCount < 3) return Task.FromResult(true);
            Drained.TrySetResult();
            return Task.FromResult(false);
        }
    }
}

using System.Threading.Channels;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 本地消息投递唤醒信号。容量为 1，用于合并高并发 PublishAsync 产生的重复唤醒。
/// </summary>
internal sealed class LocalMessageDispatchSignal : IEventBusDispatchSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public void Notify()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var delayTask = Task.Delay(timeout, cancellationToken);
        var signalTask = _channel.Reader.WaitToReadAsync(cancellationToken).AsTask();

        var completedTask = await Task.WhenAny(signalTask, delayTask);
        if (completedTask != signalTask || !await signalTask)
        {
            return false;
        }

        while (_channel.Reader.TryRead(out _))
        {
        }

        return true;
    }
}

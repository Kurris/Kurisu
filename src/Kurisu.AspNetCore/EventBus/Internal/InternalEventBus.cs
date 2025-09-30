using System.Threading.Tasks;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Internal;

/// <summary>
/// 事件总线
/// </summary>
[SkipScan]
internal class InternalEventBus : IEventBus
{
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : IAsyncChannelMessage
    {
        return AsyncChannelContext<TMessage>.PublishAsync(message);
    }
}
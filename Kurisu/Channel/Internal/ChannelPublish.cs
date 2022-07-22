using System.Threading.Tasks;
using Kurisu.Channel.Abstractions;

namespace Kurisu.Channel.Internal
{
    internal class ChannelPublish : IChannel
    {
        public async Task PublishAsync<TMessage>(TMessage message) where TMessage : IChannelMessage
        {
            await ChannelContext<TMessage>.PublishAsync(message);
        }
    }
}
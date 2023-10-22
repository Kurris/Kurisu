using System.Threading.Tasks;
using Kurisu.Channel.Abstractions;

namespace Kurisu.Channel.Internal;

/// <summary>
/// 管道推送器
/// </summary>
internal class ChannelPublisher : IChannelPublisher
{
    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="TMessage"></typeparam>
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : IChannelMessage
    {
        await ChannelContext<TMessage>.PublishAsync(message);
    }
}
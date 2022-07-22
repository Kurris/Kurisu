using System.Threading.Tasks;

namespace Kurisu.Channel.Abstractions
{
    public interface IChannel
    {
        /// <summary>
        /// 消息发布到channel
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        Task PublishAsync<TMessage>(TMessage message) where TMessage : IChannelMessage;
    }
}
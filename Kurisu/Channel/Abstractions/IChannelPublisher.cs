using System.Threading.Tasks;

namespace Kurisu.Channel.Abstractions;

/// <summary>
/// 管道数据推送器
/// </summary>
public interface IChannelPublisher
{
    /// <summary>
    /// 消息发布到channel
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <returns></returns>
    Task PublishAsync<TMessage>(TMessage message) where TMessage : IChannelMessage;
}
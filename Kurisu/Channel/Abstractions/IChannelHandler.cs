using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Channel.Abstractions
{
    /// <summary>
    /// channel 消息处理
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IChannelHandler<in TMessage> : ISingletonDependency where TMessage : IChannelMessage
    {
        Task InvokeAsync(TMessage argument);
    }
}
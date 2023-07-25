using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Channel.Abstractions;

/// <summary>
/// channel 消息处理
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IChannelHandler<in TMessage> : ISingletonDependency where TMessage : IChannelMessage
{
    /// <summary>
    /// 接受管道信息并且执行方法
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    Task InvokeAsync(TMessage argument);
}
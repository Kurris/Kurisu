using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Abstractions.Handler;

/// <summary>
/// channel 消息处理
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IAsyncChannelHandler<in TMessage> : ISingletonDependency where TMessage : IAsyncChannelMessage
{
    /// <summary>
    /// 接受管道信息并且执行方法
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task InvokeAsync(TMessage message);
}
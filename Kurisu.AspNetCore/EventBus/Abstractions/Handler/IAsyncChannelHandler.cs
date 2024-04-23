using System;
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
    /// 执行
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task InvokeAsync(IServiceProvider serviceProvider, TMessage message);
}
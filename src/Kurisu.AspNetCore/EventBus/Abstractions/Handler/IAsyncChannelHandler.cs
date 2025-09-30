using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Abstractions.Handler;

/// <summary>
/// channel 消息处理
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IAsyncChannelHandler<in TMessage>
{
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task TodoAsync(IServiceProvider serviceProvider, TMessage message);
}

/// <summary>
/// channel message
/// </summary>
public interface IAsyncChannelMessage
{
    
}
using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;
using Microsoft.Extensions.Logging;

namespace Kurisu.AspNetCore.EventBus.Abstractions;

/// <summary>
/// 通用channel handler
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class CommonChannelHandler<TMessage> : IAsyncChannelHandler<TMessage>
{
    /// <summary>
    /// logger
    /// </summary>
    protected ILogger<CommonChannelHandler<TMessage>> Logger { get; }

    /// <summary>
    /// scope service provider
    /// </summary>
    protected IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    protected CommonChannelHandler(ILogger<CommonChannelHandler<TMessage>> logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task TodoAsync(IServiceProvider serviceProvider, TMessage message)
    {
        ServiceProvider = serviceProvider;

        var name = this.GetType().Name;
        Logger.LogInformation("Channel handler: {name}", name);

        await InvokeAsync(message);
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected abstract Task InvokeAsync(TMessage message);
}
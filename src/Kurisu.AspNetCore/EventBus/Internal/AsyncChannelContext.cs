using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;
using Kurisu.AspNetCore.Scope;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Internal;

/// <summary>
/// channel 上下文
/// </summary>
/// <typeparam name="TMessage"></typeparam>
[SkipScan]
internal static class AsyncChannelContext<TMessage> where TMessage : IAsyncChannelMessage
{
    internal static async Task PublishAsync(TMessage message)
    {
        await _boundedChannel.Value.Writer.WriteAsync(message);
    }

    /// <summary>
    /// 通过懒加载创建有限容量通道
    /// </summary>
    /// <remarks>默认容量为 1000</remarks>
    private static readonly Lazy<Channel<TMessage>> _boundedChannel = new(() =>
    {
        var channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // 允许多个管道读写，提供管道吞吐量（无序操作）
            SingleWriter = false
        });

        StartReader(channel);
        return channel;
    });


    /// <summary>
    /// 开始读取
    /// </summary>
    /// <param name="channel"></param>
    private static void StartReader(Channel<TMessage, TMessage> channel)
    {
        var reader = channel.Reader;

        //创建长时间线程管道读取器
        _ = Task.Factory.StartNew(async () =>
        {
            while (await reader.WaitToReadAsync())
            {
                if (!reader.TryRead(out var message)) continue;

                _ = Task.Run(() =>
                {
                    return Scoped.Temp.Value.Invoke(sp =>
                    {
                        Console.WriteLine(message.ToString());
                        var handlers = sp.GetServices<IAsyncChannelHandler<TMessage>>().ToArray();
                        return handlers.Any()
                            ? Task.WhenAll(handlers.Select(x => x.TodoAsync(sp, message)).ToArray())
                            : Task.CompletedTask;
                    });
                });
            }
        }, TaskCreationOptions.LongRunning); //LongRunning单独一个线程处理Channel任务
    }
}
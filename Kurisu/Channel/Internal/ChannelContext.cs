using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Kurisu.Channel.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Channel.Internal
{
    /// <summary>
    /// channel 上下文
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    [SkipScan]
    internal class ChannelContext<TMessage> where TMessage : IChannelMessage
    {
        internal static async Task PublishAsync(TMessage message)
        {
            await BoundedChannel.Value.Writer.WriteAsync(message);
        }

        /// <summary>
        /// 通过懒加载创建有限容量通道
        /// </summary>
        /// <remarks>默认容量为 1000</remarks>
        private static readonly Lazy<Channel<TMessage>> BoundedChannel = new(() =>
        {
            var channel = System.Threading.Channels.Channel.CreateBounded<TMessage>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false, // 允许多个管道读写，提供管道吞吐量（无序操作）
                SingleWriter = false
            });

            StartReader(channel);
            return channel;
        });


        /// <summary>                                                                                                                
        /// 创建一个读取器
        /// </summary>
        /// <param name="channel"></param>
        private static void StartReader(Channel<TMessage, TMessage> channel)
        {
            var reader = channel.Reader;

            // 创建长时间线程管道读取器
            _ = Task.Factory.StartNew(async () =>
            {
                while (await reader.WaitToReadAsync())
                {
                    if (!reader.TryRead(out var message)) continue;
                    var task = new Task(Action);
                    task.Start();

                    // 并行执行（非等待）
                    async void Action()
                    {
                        var handler = InternalApp.ApplicationServices.GetService<IChannelHandler<TMessage>>();
                        if (handler != null)
                        {
                            await handler.InvokeAsync(message);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning); //LongRunning单独一个线程处理Channel任务
        }
    }
}
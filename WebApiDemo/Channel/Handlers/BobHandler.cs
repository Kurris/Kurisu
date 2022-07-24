using System;
using System.Threading.Tasks;
using Kurisu.Channel.Abstractions;
using Kurisu.Utils.Extensions;
using WebApiDemo.Channel.Messages;
using WebApiDemo.Services.DI;

namespace WebApiDemo.Channel.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class BobHandler : IChannelHandler<BobMessage>
    {
        private readonly ISingletonService _singletonService;

        public BobHandler(ISingletonService singletonService)
        {
            _singletonService = singletonService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        public async Task InvokeAsync(BobMessage argument)
        {
            Console.WriteLine(argument.ToJson());
            await Task.CompletedTask;
        }
    }
}
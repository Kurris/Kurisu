using Kurisu.Channel.Abstractions;
using Kurisu.Channel.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Channel.Extensions
{
    /// <summary>
    /// channel扩展类
    /// </summary>
    public static class ChannelServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuChannel(this IServiceCollection services)
        {
            services.AddSingleton<IChannel, ChannelPublish>();
            return services;
        }
    }
}
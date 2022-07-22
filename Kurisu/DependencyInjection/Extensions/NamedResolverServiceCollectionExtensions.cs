using Kurisu.DependencyInjection.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    internal static class NamedResolverServiceCollectionExtensions
    {
        /// <summary>
        /// 命名服务扩展类
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        internal static IServiceCollection AddNamedResolver(this IServiceCollection services)
        {
            services.AddTransient<INamedResolver, NamedResolver>();

            return services;
        }
    }
}
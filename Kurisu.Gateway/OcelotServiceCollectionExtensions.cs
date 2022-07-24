using Kurisu.Gateway.Services.Internals;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;

namespace Kurisu.Gateway
{
    public static class OcelotServiceCollectionExtensions
    {
        public static IServiceCollection AddKurisuOcelot(this IServiceCollection services)
        {
            services.AddOcelot();
            services.AddKurisuOptions<GatewaySetting>();

            //重写提取Ocelot配置信息
            services.AddSingleton(DataBaseConfigurationProvider.Get);
            //builder.Services.AddHostedService<FileConfigurationPoller>();
            services.AddSingleton<IFileConfigurationRepository, MySqlConfigurationRepository>();

            //注入自定义限流配置
            //注入认证信息

            return services;
        }
    }
}

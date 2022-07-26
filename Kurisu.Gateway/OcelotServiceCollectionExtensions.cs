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
            //swagger 不显示fileConfiguration接口
            // var descriptor = ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>();
            // var needToRemove = services.First(x => x.ServiceType == descriptor.ServiceType);
            // services.Remove(needToRemove);
            //
            // services.AddTransient<DefaultApiDescriptionProvider>();
            // services.TryAddEnumerable(
            //     ServiceDescriptor.Transient<IApiDescriptionProvider, OcelotApiDescriptionProvider>()
            // );

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
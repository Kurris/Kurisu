using System;
using System.Linq;
using System.Reflection;
using Kurisu.ConfigurableOptions.Abstractions;
using Kurisu.ConfigurableOptions.Attributes;
using Kurisu.Cors;
using Kurisu.DependencyInjection.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.ConfigurableOptions.Extensions
{
    /// <summary>
    /// 配置文件扩展类
    /// </summary>
    [SkipScan]
    public static class ConfigurationServiceCollectionExtensions
    {
        /// <summary>
        /// 添加所有配置文件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAllConfigurationWithConfigurationAttribute(this IServiceCollection services)
        {
            var appSettings = App.ActiveTypes.Where(x => x.IsDefined(typeof(ConfigurationAttribute), false));
            if (!appSettings.Any()) return services;

            var configuration = App.Configuration;
            var config = typeof(OptionsConfigurationServiceCollectionExtensions).GetRuntimeMethod("Configure", new[] { typeof(IServiceCollection), typeof(IConfiguration) });

            foreach (var appSetting in appSettings)
            {
                //取ConfigurationAttribute
                var configurationAttribute = appSetting.GetCustomAttribute<ConfigurationAttribute>(false);
                var configSectionPath = configurationAttribute?.SectionPath;

                //配置路径
                var sectionPath = string.IsNullOrEmpty(configSectionPath)
                    ? appSetting.Name
                    : configSectionPath;

                var section = configuration.GetSection(sectionPath);
                
                //绑定配置
                config?.MakeGenericMethod(appSetting).Invoke(null, new object[] { services, section });
            }

            return services;
        }

        /// <summary>
        /// 添加配置选项
        /// </summary>
        /// <param name="serivces">服务容器</param>
        /// <typeparam name="TOptions">选项类型</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddConfigurableOptions<TOptions>(this IServiceCollection serivces)
            where TOptions : class, new()
        {
            var optionsType = typeof(TOptions);

            //取ConfigurationAttribute
            var configurationAttribute = optionsType.GetCustomAttribute<ConfigurationAttribute>(true);
            var configSectionPath = configurationAttribute?.SectionPath;

            //配置路径
            var sectionPath = string.IsNullOrEmpty(configSectionPath)
                ? optionsType.Name
                : configSectionPath;

            var configuration = App.Configuration;
            var section = configuration.GetSection(sectionPath);

            //绑定配置
            var builder = serivces.AddOptions<TOptions>()
                .Bind(section)
                .ValidateDataAnnotations(); //配置验证

            //检查是否有后置配置
            if (typeof(IPostConfigure<TOptions>).IsAssignableFrom(optionsType))
            {
                builder.PostConfigure(options => { (options as IPostConfigure<TOptions>)?.PostConfigure(configuration, options); });
            }

            serivces.Configure<TOptions>(configuration.GetSection(""));
            return serivces;
        }
    }
}
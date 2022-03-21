using System;
using System.Linq;
using System.Reflection;
using Kurisu.ConfigurableOptions.Abstractions;
using Kurisu.ConfigurableOptions.Attributes;
using Kurisu.Cors;
using Kurisu.DependencyInjection.Attributes;
using Kurisu.UnifyResultAndValidation;
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
        public static IServiceCollection AddAppSettingMapping(this IServiceCollection services)
        {
            var typesNeedToMapping = App.ActiveTypes.Where(x => x.IsDefined(typeof(ConfigurationAttribute), false));
            if (!typesNeedToMapping.Any()) return services;

            var configuration = App.Configuration;
            var method = typeof(OptionsConfigurationServiceCollectionExtensions).GetRuntimeMethod("Configure",
                new[]
                {
                    typeof(IServiceCollection), typeof(IConfiguration)

                });

            foreach (var type in typesNeedToMapping)
            {
                //取ConfigurationAttribute
                var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>(false);
                
                //配置路径
                var sectionPath = string.IsNullOrEmpty(configurationAttribute.Section)
                    ? type.Name
                    : configurationAttribute.Section;
                
                var section = configuration.GetSection(sectionPath);
                
                //绑定配置
                method?.MakeGenericMethod(type).Invoke(null, new object[] {services, section});
            }

            return services;
        }

        /// <summary>
        /// 添加配置选项,验证配置
        /// </summary>
        /// <param name="services">服务容器</param>
        /// <typeparam name="TOptions">选项类型</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddOptionsMapping<TOptions>(this IServiceCollection services)  where TOptions : class, new()
        {
            var optionsType = typeof(TOptions);

            string sectionPath;
            if (!optionsType.IsDefined(typeof(ConfigurationAttribute),false))
            {
                sectionPath = optionsType.Name;
            }
            else
            {
                //取ConfigurationAttribute
                var configurationAttribute = optionsType.GetCustomAttribute<ConfigurationAttribute>(false);
                //配置路径
                sectionPath =  string.IsNullOrEmpty(configurationAttribute.Section)
                    ? optionsType.Name
                    : configurationAttribute.Section;
            }

            
            var configuration = App.Configuration;
            var section = configuration.GetSection(sectionPath);

            //绑定配置
            var builder = services.AddOptions<TOptions>()
                .Bind(section)
                .ValidateDataAnnotations(); //配置验证

            //检查是否有后置配置
            if (typeof(IPostConfigure<TOptions>).IsAssignableFrom(optionsType))
            {
                builder.PostConfigure(options => { (options as IPostConfigure<TOptions>)?.PostConfigure(configuration, options); });
            }
            
            return services;
        }
    }
}
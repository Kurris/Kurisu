using System.Linq;
using System.Reflection;
using Kurisu;
using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
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
        public static IServiceCollection AddKurisuConfiguration(this IServiceCollection services)
        {
            var typesNeedToMapping = App.ActiveTypes.Where(x => x.IsDefined(typeof(ConfigurationAttribute)));
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
                var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>();

                //配置路径
                var path = string.IsNullOrEmpty(configurationAttribute.Path)
                    ? type.Name
                    : configurationAttribute.Path;

                var section = configuration.GetSection(path);
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
        public static IServiceCollection AddKurisuOptions<TOptions>(this IServiceCollection services) where TOptions : class, new()
        {
            var optionsType = typeof(TOptions);

            string path;
            if (!optionsType.IsDefined(typeof(ConfigurationAttribute), false))
            {
                path = optionsType.Name;
            }
            else
            {
                //取ConfigurationAttribute
                var configurationAttribute = optionsType.GetCustomAttribute<ConfigurationAttribute>();
                //配置路径
                path = string.IsNullOrEmpty(configurationAttribute.Path)
                    ? optionsType.Name
                    : configurationAttribute.Path;
            }

            var configuration = App.Configuration;
            var section = configuration.GetSection(path);

            //绑定配置
            services.AddOptions<TOptions>()
                .Bind(section)
                .ValidateDataAnnotations(); //配置验证


            return services;
        }
    }
}
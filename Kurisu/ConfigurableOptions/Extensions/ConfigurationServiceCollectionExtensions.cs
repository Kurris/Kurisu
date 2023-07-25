using System.Linq;
using System.Reflection;
using Kurisu;
using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddKurisuConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var typesNeedToMapping = App.ActiveTypes.Where(x => x.IsDefined(typeof(ConfigurationAttribute))).ToArray();
        if (!typesNeedToMapping.Any()) return services;

        //Configure
        var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions).GetRuntimeMethod(nameof(OptionsConfigurationServiceCollectionExtensions.Configure),
            new[] {typeof(IServiceCollection), typeof(IConfiguration)})!;
        //ValidateDataAnnotations
        var validateDataAnnotationsMethod = typeof(OptionsBuilderDataAnnotationsExtensions).GetMethod(nameof(OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations))!;

        //Options
        var addOptionsMethod = typeof(OptionsServiceCollectionExtensions)
            .GetRuntimeMethods().First(x => x.Name.Equals(nameof(OptionsServiceCollectionExtensions.AddOptions)) && x.IsGenericMethod);

        var bind = typeof(OptionsBuilderConfigurationExtensions)
            .GetMethods()
            .Where(x => x.Name.Equals(nameof(OptionsBuilderConfigurationExtensions.Bind)))
            .Where(x => x.IsGenericMethod)
            .First(x => x.GetParameters().Length == 2 && x.GetParameters().Last().ParameterType == typeof(IConfiguration));


        foreach (var type in typesNeedToMapping)
        {
            var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>()!;

            //配置路径
            var path = string.IsNullOrEmpty(configurationAttribute.Path)
                ? type.Name
                : configurationAttribute.Path;

            var section = configuration.GetSection(path);
            //绑定配置  services.Configure<T>(section);
            configureMethod.MakeGenericMethod(type).Invoke(null, new object[] {services, section});

            //绑定配置 services.AddOptions<T>().Bind(section).ValidateDataAnnotations()
            var optionsBuilder = addOptionsMethod.MakeGenericMethod(type).Invoke(null, new object[] {services});
            optionsBuilder = bind.MakeGenericMethod(type).Invoke(null, new[] {optionsBuilder, section});
            validateDataAnnotationsMethod.MakeGenericMethod(type).Invoke(null, new[] {optionsBuilder});
        }

        return services;
    }
}
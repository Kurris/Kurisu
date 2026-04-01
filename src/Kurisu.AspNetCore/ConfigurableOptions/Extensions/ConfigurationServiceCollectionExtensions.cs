using System;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var types = DependencyInjectionHelper.Configurations.Value;
        if (types.Count == 0) return services;


        //先处理后置配置
        var postConfigureOptions = types.Where(x =>
            x.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>))
        ).ToList();
        types.RemoveAll(postConfigureOptions.Contains);

        //Options
        var addOptionsMethod = typeof(OptionsServiceCollectionExtensions)
            .GetRuntimeMethods().First(x => x.Name.Equals(nameof(OptionsServiceCollectionExtensions.AddOptions)) && x.IsGenericMethod);

        var bind = typeof(OptionsBuilderConfigurationExtensions)
            .GetMethods()
            .Where(x => x.Name.Equals(nameof(OptionsBuilderConfigurationExtensions.Bind)))
            .Where(x => x.IsGenericMethod)
            .First(x => x.GetParameters().Length == 2 && x.GetParameters().Last().ParameterType == typeof(IConfiguration));

        //ValidateDataAnnotations
        var validateDataAnnotationsMethod = typeof(OptionsBuilderDataAnnotationsExtensions).GetMethod(nameof(OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations))!;

        var get = typeof(ConfigurationBinder)
            .GetMethods()
            .Where(x => x.Name.Equals(nameof(ConfigurationBinder.Get)))
            .First(x => x.IsGenericMethod);

        var getType = typeof(ConfigurationServiceCollectionExtensions).GetMethod(nameof(GetType), BindingFlags.NonPublic | BindingFlags.Static)!;
        var startupConfigure = typeof(ConfigurationServiceCollectionExtensions).GetMethod(nameof(StartupConfigure), BindingFlags.NonPublic | BindingFlags.Static)!;

        foreach (var type in types)
        {
            var path = type.GetSectionPath();
            var section = configuration.GetSection(path);

            //绑定配置 services.AddOptions<T>().Bind(section).ValidateDataAnnotations()
            var optionsBuilder = addOptionsMethod.MakeGenericMethod(type).Invoke(null, [services]);
            optionsBuilder = bind.MakeGenericMethod(type).Invoke(null, [optionsBuilder, section]);
            validateDataAnnotationsMethod.MakeGenericMethod(type).Invoke(null, [optionsBuilder]);

            if (type.IsAssignableTo((Type)getType.MakeGenericMethod(type).Invoke(null, null)!))
            {
                // 构造 Action<T> 类型
                var actionType = typeof(Action<>).MakeGenericType(type);
                var PostConfigureMethod = optionsBuilder.GetType().GetMethod(nameof(OptionsBuilder<>.PostConfigure), new Type[] { actionType })!;

                var del = Delegate.CreateDelegate(actionType, null, startupConfigure.MakeGenericMethod(type));
                PostConfigureMethod.Invoke(optionsBuilder, [del]);
            }
        }


        foreach (var item in postConfigureOptions)
        {
            var @interface = item.GetInterfaces().First(x => x.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>));
            services.TryAddEnumerable(ServiceDescriptor.Singleton(@interface, item));
        }


        return services;
    }

    private static Type GetType<T>() where T : class
    {
        return typeof(IStartupConfigure<T>);
    }

    private static void StartupConfigure<T>(T value) where T : class
    {
        if (value is IStartupConfigure<T> configure)
        {
            configure.StartupConfigure(value);
        }
    }

    private static string GetSectionPath(this Type type)
    {
        var attribute = type.GetCustomAttribute<ConfigurationAttribute>()!;

        //配置路径
        var path = string.IsNullOrEmpty(attribute.Path)
            ? type.Name
            : attribute.Path;

        return path;
    }
}
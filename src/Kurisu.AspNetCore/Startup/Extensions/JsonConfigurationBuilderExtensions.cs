using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.DependencyInjection;
using Microsoft.Extensions.FileProviders;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration;

/// <summary>
/// JSON 配置扩展。
/// </summary>
[SkipScan]
public static class JsonConfigurationBuilderExtensions
{
    /// <summary>
    /// 添加 Kurisu 自定义 JSON 配置文件。
    /// </summary>
    /// <param name="configurationBuilder">配置构建器。</param>
    /// <param name="environmentName">当前环境名称。</param>
    /// <returns>配置构建器。</returns>
    public static IConfigurationBuilder AddKurisuJsonConfigurationFiles(
        this IConfigurationBuilder configurationBuilder,
        string environmentName)
    {
        var fileProvider = new PhysicalFileProvider(AppContext.BaseDirectory);
        foreach (var file in GetJsonConfigurationFiles(environmentName))
        {
            if (Path.IsPathRooted(file.Path))
            {
                configurationBuilder.AddJsonFile(file.Path, file.Optional, file.ReloadOnChange);
                continue;
            }

            configurationBuilder.AddJsonFile(fileProvider, file.Path, file.Optional, file.ReloadOnChange);
        }

        return configurationBuilder;
    }

    private static IEnumerable<JsonConfigurationFile> GetJsonConfigurationFiles(string environmentName)
    {
        var providers = DependencyInjectionHelper.ActiveTypes.Value
            .Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false })
            .Where(x => typeof(IJsonConfigurationProvider).IsAssignableFrom(x))
            .Select(x => (IJsonConfigurationProvider)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Order);

        foreach (var provider in providers)
        {
            foreach (var file in provider.GetJsonFiles(environmentName) ?? Enumerable.Empty<JsonConfigurationFile>())
            {
                yield return file;
            }
        }
    }
}

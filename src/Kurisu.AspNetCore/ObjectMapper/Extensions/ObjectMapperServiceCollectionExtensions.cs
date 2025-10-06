using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 对象关系映射扩展类
/// </summary>
[SkipScan]
public static class ObjectMapperServiceCollectionExtensions
{
    /// <summary>
    /// 添加对象关系映射
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <param name="isCompile">是否提前编译</param>
    /// <param name="assemblies">被扫描的程序集</param>
    /// <returns></returns>
    public static IServiceCollection AddObjectMapper(this IServiceCollection services, bool isCompile = false, params Assembly[] assemblies)
    {
        var globalSettings = TypeAdapterConfig.GlobalSettings;

        //扫描IRegister
        if (assemblies.Any())
            globalSettings.Scan(assemblies); //IRegister

        //名称匹配规则
        globalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);

        //globalSettings 一定要单例注入
        services.TryAddSingleton(globalSettings);
        services.TryAddSingleton<IMapper, ServiceMapper>();

        //提前编译会增加内容
        if (isCompile)
            globalSettings.Compile();

        return services;
    }
}
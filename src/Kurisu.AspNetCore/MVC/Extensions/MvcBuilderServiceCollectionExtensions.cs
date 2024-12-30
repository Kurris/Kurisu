using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MVC扩展类
/// </summary>
[SkipScan]
public static class MvcBuilderServiceCollectionExtensions
{
    /// <summary>
    /// 注册过滤器
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TFilter"></typeparam>
    /// <returns></returns>
    public static IMvcBuilder AddMvcFilter<TFilter>(this IMvcBuilder mvcBuilder, Action<MvcOptions> configure = null)
        where TFilter : IFilterMetadata
    {
        mvcBuilder.Services.AddMvcFilter<TFilter>(configure);
        return mvcBuilder;
    }

    /// <summary>
    /// 注册过滤器
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TFilter"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddMvcFilter<TFilter>(this IServiceCollection services, Action<MvcOptions> configure = null)
        where TFilter : IFilterMetadata
    {
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<TFilter>(); // 使用ActivatorUtilities
            //options.Filters.AddService() 使用依赖注入的方式

            //其他额外配置
            configure?.Invoke(options);
        });

        return services;
    }
}
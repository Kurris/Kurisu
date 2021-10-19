using System;
using Kurisu.DependencyInjection.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.MVC.Extensions
{
    /// <summary>
    /// mvc扩展类
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
        public static IMvcBuilder AddMvcFilter<TFilter>(this IMvcBuilder mvcBuilder, Action<MvcOptions> configure = default)
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
        public static IServiceCollection AddMvcFilter<TFilter>(this IServiceCollection services, Action<MvcOptions> configure = default)
            where TFilter : IFilterMetadata
        {
            // 非 Web 环境跳过注册
            // if (App.WebHostEnvironment == default) return services;

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add<TFilter>();

                // 其他额外配置
                configure?.Invoke(options);
            });

            return services;
        }
    }
}